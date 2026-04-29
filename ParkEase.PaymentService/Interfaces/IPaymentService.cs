using ParkEase.PaymentService.DTOs;

namespace ParkEase.PaymentService.Interfaces;

public interface IPaymentService
{
    Task<ApiResponse<RazorpayOrderDto>> CreateRazorpayOrderAsync(CreateOrderRequest request);
    Task<ApiResponse<PaymentDto>> VerifyAndSavePaymentAsync(VerifyPaymentRequest request);
    Task<ApiResponse<PaymentDto>> ProcessPaymentAsync(ProcessPaymentRequest request);
    Task<ApiResponse<PaymentDto>> GetByBookingIdAsync(int bookingId);
    Task<ApiResponse<List<PaymentDto>>> GetByUserIdAsync(int userId);
    Task<ApiResponse<PaymentDto>> GetByPaymentIdAsync(int paymentId);
    Task<ApiResponse<PaymentDto>> RefundPaymentAsync(RefundRequest request);
    Task<ApiResponse<string>> GetPaymentStatusAsync(int paymentId);
    Task<ApiResponse<List<PaymentDto>>> GetTransactionHistoryAsync(int userId);
}