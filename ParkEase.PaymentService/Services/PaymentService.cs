using Razorpay.Api;
using ParkEase.PaymentService.DTOs;
using ParkEase.PaymentService.Entities;
using ParkEase.PaymentService.Interfaces;
using PaymentEntity = ParkEase.PaymentService.Entities.Payment;

namespace ParkEase.PaymentService.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _repo;
    private readonly IConfiguration _config;
    private readonly string _keyId;
    private readonly string _keySecret;

    private static readonly string[] ValidModes = { "CARD", "UPI", "WALLET", "CASH" };

    public PaymentService(IPaymentRepository repo, IConfiguration config)
    {
        _repo = repo;
        _config = config;
        _keyId = config["Razorpay:KeyId"] ?? string.Empty;
        _keySecret = config["Razorpay:KeySecret"] ?? string.Empty;
    }

    public async Task<ApiResponse<RazorpayOrderDto>> CreateRazorpayOrderAsync(CreateOrderRequest request)
    {
        try
        {
            var client = new RazorpayClient(_keyId, _keySecret);
            var options = new Dictionary<string, object>
            {
                { "amount", (int)(request.Amount * 100) },
                { "currency", "INR" },
                { "receipt", $"booking_{request.BookingId}" }
            };

            var order = client.Order.Create(options);
            var orderId = order["id"].ToString();

            return ApiResponse<RazorpayOrderDto>.Ok(new RazorpayOrderDto
            {
                OrderId = orderId,
                Amount = request.Amount,
                Currency = "INR",
                KeyId = _keyId,
                BookingId = request.BookingId,
                UserId = request.UserId,
                Description = $"Parking fee for booking #{request.BookingId}"
            }, "Order created.");
        }
        catch (Exception ex)
        {
            return ApiResponse<RazorpayOrderDto>.Fail($"Failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<PaymentDto>> VerifyAndSavePaymentAsync(VerifyPaymentRequest request)
    {
        try
        {
            var attributes = new Dictionary<string, string>
            {
                { "razorpay_order_id", request.RazorpayOrderId },
                { "razorpay_payment_id", request.RazorpayPaymentId },
                { "razorpay_signature", request.RazorpaySignature }
            };

            Utils.verifyPaymentSignature(attributes);

            var existing = await _repo.FindByBookingIdAsync(request.BookingId);
            var payment = existing ?? new PaymentEntity
            {
                BookingId = request.BookingId,
                UserId = request.UserId,
                CreatedAt = DateTime.UtcNow
            };

            payment.Amount = request.Amount;
            payment.Mode = request.Mode ?? "CARD";
            payment.Status = "PAID";
            payment.TransactionId = request.RazorpayPaymentId;
            payment.Description = $"Razorpay payment for booking #{request.BookingId}";
            payment.PaidAt = DateTime.UtcNow;
            payment.Currency = "INR";

            PaymentEntity result = existing != null
                ? await _repo.UpdateAsync(payment)
                : await _repo.CreateAsync(payment);

            return ApiResponse<PaymentDto>.Ok(MapToDto(result), "Payment verified and saved!");
        }
        catch (Exception ex)
        {
            return ApiResponse<PaymentDto>.Fail($"Payment verification failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<PaymentDto>> ProcessPaymentAsync(ProcessPaymentRequest request)
    {
        if (!ValidModes.Contains(request.Mode.ToUpper()))
            return ApiResponse<PaymentDto>.Fail($"Invalid mode. Valid: {string.Join(", ", ValidModes)}");

        if (request.Amount <= 0)
            return ApiResponse<PaymentDto>.Fail("Amount must be greater than 0.");

        var existing = await _repo.FindByBookingIdAsync(request.BookingId);
        if (existing != null && existing.Status == "PAID")
            return ApiResponse<PaymentDto>.Fail("Payment already processed.");

        var payment = existing ?? new PaymentEntity
        {
            BookingId = request.BookingId,
            UserId = request.UserId,
            CreatedAt = DateTime.UtcNow
        };

        payment.Amount = request.Amount;
        payment.Mode = request.Mode.ToUpper();
        payment.Status = "PAID";
        payment.TransactionId = $"CASH-{DateTime.UtcNow.Ticks}";
        payment.Description = request.Description ?? $"Cash payment for booking #{request.BookingId}";
        payment.PaidAt = DateTime.UtcNow;
        payment.Currency = "INR";

        PaymentEntity result = existing != null
            ? await _repo.UpdateAsync(payment)
            : await _repo.CreateAsync(payment);

        return ApiResponse<PaymentDto>.Ok(MapToDto(result),
            $"Payment of ₹{request.Amount} processed.");
    }

    public async Task<ApiResponse<PaymentDto>> GetByBookingIdAsync(int bookingId)
    {
        var payment = await _repo.FindByBookingIdAsync(bookingId);
        if (payment == null) return ApiResponse<PaymentDto>.Fail("No payment found.");
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
            return ApiResponse<PaymentDto>.Fail($"Cannot refund. Status: {payment.Status}");

        if (payment.TransactionId != null && payment.TransactionId.StartsWith("pay_"))
        {
            try
            {
                var client = new RazorpayClient(_keyId, _keySecret);
                var options = new Dictionary<string, object>
                {
                    { "amount", (int)(payment.Amount * 100) }
                };
                client.Payment.Fetch(payment.TransactionId).Refund(options);
            }
            catch (Exception ex)
            {
                return ApiResponse<PaymentDto>.Fail($"Refund failed: {ex.Message}");
            }
        }

        payment.Status = "REFUNDED";
        payment.RefundedAt = DateTime.UtcNow;
        payment.Description = $"Refunded: {request.Reason}";

        var updated = await _repo.UpdateAsync(payment);
        return ApiResponse<PaymentDto>.Ok(MapToDto(updated), $"Refund processed.");
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
        return ApiResponse<List<PaymentDto>>.Ok(payments.Select(MapToDto).ToList());
    }

    private static PaymentDto MapToDto(PaymentEntity p) => new()
    {
        PaymentId = p.PaymentId,
        BookingId = p.BookingId,
        UserId = p.UserId,
        Amount = p.Amount,
        Status = p.Status,
        Mode = p.Mode,
        TransactionId = p.TransactionId,
        Currency = p.Currency,
        Description = p.Description,
        CreatedAt = p.CreatedAt,
        PaidAt = p.PaidAt,
        RefundedAt = p.RefundedAt
    };
}