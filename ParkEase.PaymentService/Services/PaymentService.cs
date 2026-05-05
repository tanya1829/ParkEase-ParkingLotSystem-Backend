using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ParkEase.PaymentService.DTOs;
using ParkEase.PaymentService.Entities;
using ParkEase.PaymentService.Interfaces;

namespace ParkEase.PaymentService.Services;

/// <summary>
/// Handles payment processing, refunds, and Razorpay integration.
/// Razorpay flow: CreateOrder → (frontend opens modal) → VerifyAndRecord
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _repo;
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PaymentService> _logger;

    private static readonly string[] ValidModes = { "CARD", "UPI", "WALLET", "CASH" };

    public PaymentService(
        IPaymentRepository repo,
        IConfiguration config,
        IHttpClientFactory httpClientFactory,
        ILogger<PaymentService> logger)
    {
        _repo = repo;
        _config = config;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    // ════════════════════════════════════════════════
    // RAZORPAY — Step 1: Create Order
    // ════════════════════════════════════════════════

    public async Task<ApiResponse<RazorpayOrderDto>> CreateRazorpayOrderAsync(CreateOrderRequest request)
    {
        if (request.Amount <= 0)
            return ApiResponse<RazorpayOrderDto>.Fail("Amount must be greater than 0.");

        var keyId     = _config["Razorpay:KeyId"];
        var keySecret = _config["Razorpay:KeySecret"];

        // Amount in paise (Razorpay expects smallest currency unit)
        var amountInPaise = (long)(request.Amount * 100);

        var orderPayload = new
        {
            amount   = amountInPaise,
            currency = "INR",
            receipt  = $"booking_{request.BookingId}_{DateTime.UtcNow.Ticks}",
            notes    = new { bookingId = request.BookingId, userId = request.UserId }
        };

        try
        {
            var client = _httpClientFactory.CreateClient("Razorpay");

            // Basic auth: KeyId:KeySecret encoded as Base64
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{keyId}:{keySecret}"));
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

            var json    = JsonSerializer.Serialize(orderPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api.razorpay.com/v1/orders", content);
            var body     = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Razorpay order creation failed: {Body}", body);
                return ApiResponse<RazorpayOrderDto>.Fail("Failed to create Razorpay order. Please try again.");
            }

            using var doc = JsonDocument.Parse(body);
            var orderId   = doc.RootElement.GetProperty("id").GetString() ?? string.Empty;

            return ApiResponse<RazorpayOrderDto>.Ok(new RazorpayOrderDto
            {
                OrderId     = orderId,
                KeyId       = keyId!,
                Amount      = request.Amount,
                Currency    = "INR",
                Description = request.Description ?? $"Parking fee for booking #{request.BookingId}"
            }, "Order created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while creating Razorpay order");
            return ApiResponse<RazorpayOrderDto>.Fail("Payment gateway error. Please try again.");
        }
    }

    // ════════════════════════════════════════════════
    // RAZORPAY — Step 2: Verify Signature + Record Payment
    // ════════════════════════════════════════════════

    public async Task<ApiResponse<PaymentDto>> VerifyAndRecordPaymentAsync(VerifyPaymentRequest request)
    {
        var keySecret = _config["Razorpay:KeySecret"] ?? string.Empty;

        // Razorpay HMAC-SHA256 signature verification
        // Expected signature = HMAC_SHA256(orderId + "|" + paymentId, keySecret)
        var payload   = $"{request.RazorpayOrderId}|{request.RazorpayPaymentId}";
        var computed  = ComputeHmacSha256(payload, keySecret);

        if (!string.Equals(computed, request.RazorpaySignature, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Razorpay signature mismatch for booking {BookingId}", request.BookingId);
            return ApiResponse<PaymentDto>.Fail("Payment verification failed. Invalid signature.");
        }

        // Signature valid — record the payment
        var existing = await _repo.FindByBookingIdAsync(request.BookingId);
        if (existing != null && existing.Status == "PAID")
            return ApiResponse<PaymentDto>.Fail("Payment already recorded for this booking.");

        var payment = existing ?? new Payment
        {
            BookingId = request.BookingId,
            UserId    = request.UserId,
            CreatedAt = DateTime.UtcNow
        };

        payment.Amount        = request.Amount;
        payment.Mode          = request.Mode.ToUpper();
        payment.Status        = "PAID";
        payment.TransactionId = request.RazorpayPaymentId;
        payment.Description   = $"Razorpay payment for booking #{request.BookingId}";
        payment.PaidAt        = DateTime.UtcNow;
        payment.Currency      = "INR";

        Payment result = existing != null
            ? await _repo.UpdateAsync(payment)
            : await _repo.CreateAsync(payment);

        _logger.LogInformation(
            "Payment recorded: BookingId={BookingId}, TxnId={TxnId}, Amount={Amount}",
            request.BookingId, request.RazorpayPaymentId, request.Amount);

        return ApiResponse<PaymentDto>.Ok(MapToDto(result),
            $"Payment of ₹{request.Amount} verified and recorded successfully.");
    }

    // ════════════════════════════════════════════════
    // EXISTING METHODS (unchanged)
    // ════════════════════════════════════════════════

    public async Task<ApiResponse<PaymentDto>> ProcessPaymentAsync(ProcessPaymentRequest request)
    {
        if (!ValidModes.Contains(request.Mode.ToUpper()))
            return ApiResponse<PaymentDto>.Fail($"Invalid payment mode. Valid: {string.Join(", ", ValidModes)}");

        if (request.Amount <= 0)
            return ApiResponse<PaymentDto>.Fail("Amount must be greater than 0.");

        var existing = await _repo.FindByBookingIdAsync(request.BookingId);
        if (existing != null && existing.Status == "PAID")
            return ApiResponse<PaymentDto>.Fail("Payment already processed for this booking.");

        var transactionId = request.Mode.ToUpper() != "CASH"
            ? $"TXN-{DateTime.UtcNow.Ticks}-{request.BookingId}"
            : null;

        var payment = existing ?? new Payment
        {
            BookingId = request.BookingId,
            UserId    = request.UserId,
            CreatedAt = DateTime.UtcNow
        };

        payment.Amount        = request.Amount;
        payment.Mode          = request.Mode.ToUpper();
        payment.Status        = "PAID";
        payment.TransactionId = transactionId;
        payment.Description   = request.Description ?? $"Parking fee for booking #{request.BookingId}";
        payment.PaidAt        = DateTime.UtcNow;
        payment.Currency      = "INR";

        Payment result = existing != null
            ? await _repo.UpdateAsync(payment)
            : await _repo.CreateAsync(payment);

        return ApiResponse<PaymentDto>.Ok(MapToDto(result),
            $"Payment of ₹{request.Amount} processed successfully via {request.Mode.ToUpper()}.");
    }

    public async Task<ApiResponse<PaymentDto>> GetByBookingIdAsync(int bookingId)
    {
        var payment = await _repo.FindByBookingIdAsync(bookingId);
        if (payment == null) return ApiResponse<PaymentDto>.Fail("No payment found for this booking.");
        return ApiResponse<PaymentDto>.Ok(MapToDto(payment));
    }

    public async Task<ApiResponse<List<PaymentDto>>> GetByUserIdAsync(int userId)
    {
        var payments = await _repo.FindByUserIdAsync(userId);
        return ApiResponse<List<PaymentDto>>.Ok(payments.Select(MapToDto).ToList());
    }

    public async Task<ApiResponse<PaymentDto>> GetByPaymentIdAsync(int paymentId)
    {
        var payment = await _repo.FindByPaymentIdAsync(paymentId);
        if (payment == null) return ApiResponse<PaymentDto>.Fail("Payment not found.");
        return ApiResponse<PaymentDto>.Ok(MapToDto(payment));
    }

    public async Task<ApiResponse<PaymentDto>> RefundPaymentAsync(RefundRequest request)
    {
        var payment = await _repo.FindByPaymentIdAsync(request.PaymentId);
        if (payment == null) return ApiResponse<PaymentDto>.Fail("Payment not found.");

        if (payment.Status != "PAID")
            return ApiResponse<PaymentDto>.Fail($"Cannot refund. Payment status is {payment.Status}.");

        payment.Status      = "REFUNDED";
        payment.RefundedAt  = DateTime.UtcNow;
        payment.Description = $"Refunded: {request.Reason}";

        var updated = await _repo.UpdateAsync(payment);
        return ApiResponse<PaymentDto>.Ok(MapToDto(updated),
            $"Refund of ₹{payment.Amount} processed successfully.");
    }

    public async Task<ApiResponse<string>> GetPaymentStatusAsync(int paymentId)
    {
        var payment = await _repo.FindByPaymentIdAsync(paymentId);
        if (payment == null) return ApiResponse<string>.Fail("Payment not found.");
        return ApiResponse<string>.Ok(payment.Status);
    }

    public async Task<ApiResponse<List<PaymentDto>>> GetTransactionHistoryAsync(int userId)
    {
        var payments = await _repo.FindByUserIdAsync(userId);
        return ApiResponse<List<PaymentDto>>.Ok(
            payments.Select(MapToDto).ToList(),
            $"{payments.Count} transactions found.");
    }

    // ── Private Helpers ──────────────────────────────────────────────────────

    private static string ComputeHmacSha256(string data, string secret)
    {
        var key   = Encoding.UTF8.GetBytes(secret);
        var bytes = Encoding.UTF8.GetBytes(data);
        using var hmac = new HMACSHA256(key);
        return Convert.ToHexString(hmac.ComputeHash(bytes)).ToLower();
    }

    private static PaymentDto MapToDto(Payment p) => new()
    {
        PaymentId     = p.PaymentId,
        BookingId     = p.BookingId,
        UserId        = p.UserId,
        Amount        = p.Amount,
        Status        = p.Status,
        Mode          = p.Mode,
        TransactionId = p.TransactionId,
        Currency      = p.Currency,
        Description   = p.Description,
        CreatedAt     = p.CreatedAt,
        PaidAt        = p.PaidAt,
        RefundedAt    = p.RefundedAt
    };
}