using ParkEase.PaymentService.DTOs;
using ParkEase.PaymentService.Entities;
using ParkEase.PaymentService.Interfaces;

namespace ParkEase.PaymentService.Services;

/// <summary>
/// Handles payment processing, refunds, and transaction history.
/// Supports CARD, UPI, WALLET, and CASH payment modes.
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _repo;

    private static readonly string[] ValidModes = { "CARD", "UPI", "WALLET", "CASH" };

    public PaymentService(IPaymentRepository repo)
    {
        _repo = repo;
    }

    public async Task<ApiResponse<PaymentDto>> ProcessPaymentAsync(ProcessPaymentRequest request)
    {
        // Validate payment mode
        if (!ValidModes.Contains(request.Mode.ToUpper()))
            return ApiResponse<PaymentDto>.Fail($"Invalid payment mode. Valid: {string.Join(", ", ValidModes)}");

        // Validate amount
        if (request.Amount <= 0)
            return ApiResponse<PaymentDto>.Fail("Amount must be greater than 0.");

        // Check if payment already exists for this booking
        var existing = await _repo.FindByBookingIdAsync(request.BookingId);
        if (existing != null && existing.Status == "PAID")
            return ApiResponse<PaymentDto>.Fail("Payment already processed for this booking.");

        // Generate transaction ID for non-cash payments
        var transactionId = request.Mode.ToUpper() != "CASH"
            ? $"TXN-{DateTime.UtcNow.Ticks}-{request.BookingId}"
            : null;

        var payment = existing ?? new Payment
        {
            BookingId = request.BookingId,
            UserId = request.UserId,
            CreatedAt = DateTime.UtcNow
        };

        payment.Amount = request.Amount;
        payment.Mode = request.Mode.ToUpper();
        payment.Status = "PAID";
        payment.TransactionId = transactionId;
        payment.Description = request.Description ?? $"Parking fee for booking #{request.BookingId}";
        payment.PaidAt = DateTime.UtcNow;
        payment.Currency = "INR";

        Payment result;
        if (existing != null)
            result = await _repo.UpdateAsync(payment);
        else
            result = await _repo.CreateAsync(payment);

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

        payment.Status = "REFUNDED";
        payment.RefundedAt = DateTime.UtcNow;
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

    // ---- Private Helper ----
    private static PaymentDto MapToDto(Payment p) => new()
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
