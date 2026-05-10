using ParkEase.PaymentService.Entities;

namespace ParkEase.PaymentService.Interfaces;

/// <summary>Data access contract for payment operations</summary>
public interface IPaymentRepository
{
    Task<Payment?> FindByBookingIdAsync(int bookingId);
    Task<List<Payment>> FindByUserIdAsync(int userId);
    Task<Payment?> FindByPaymentIdAsync(int paymentId);
    Task<List<Payment>> FindByStatusAsync(string status);
    Task<Payment?> FindByTransactionIdAsync(string transactionId);
    Task<decimal> SumAmountByUserIdAsync(int userId);
    Task<int> CountByUserIdAsync(int userId);
    Task<Payment> CreateAsync(Payment payment);
    Task<Payment> UpdateAsync(Payment payment);
}
