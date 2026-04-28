using Microsoft.EntityFrameworkCore;
using ParkEase.PaymentService.Data;
using ParkEase.PaymentService.Entities;
using ParkEase.PaymentService.Interfaces;

namespace ParkEase.PaymentService.Repositories;

/// <summary>EF Core implementation of payment data access</summary>
public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _context;

    public PaymentRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> FindByBookingIdAsync(int bookingId) =>
        await _context.Payments.FirstOrDefaultAsync(p => p.BookingId == bookingId);

    public async Task<List<Payment>> FindByUserIdAsync(int userId) =>
        await _context.Payments
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    public async Task<Payment?> FindByPaymentIdAsync(int paymentId) =>
        await _context.Payments.FindAsync(paymentId);

    public async Task<List<Payment>> FindByStatusAsync(string status) =>
        await _context.Payments
            .Where(p => p.Status == status)
            .ToListAsync();

    public async Task<Payment?> FindByTransactionIdAsync(string transactionId) =>
        await _context.Payments
            .FirstOrDefaultAsync(p => p.TransactionId == transactionId);

    public async Task<decimal> SumAmountByUserIdAsync(int userId) =>
        await _context.Payments
            .Where(p => p.UserId == userId && p.Status == "PAID")
            .SumAsync(p => p.Amount);

    public async Task<int> CountByUserIdAsync(int userId) =>
        await _context.Payments.CountAsync(p => p.UserId == userId);

    public async Task<Payment> CreateAsync(Payment payment)
    {
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        return payment;
    }

    public async Task<Payment> UpdateAsync(Payment payment)
    {
        _context.Payments.Update(payment);
        await _context.SaveChangesAsync();
        return payment;
    }
}
