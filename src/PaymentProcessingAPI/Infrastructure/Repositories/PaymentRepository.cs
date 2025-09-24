using Microsoft.EntityFrameworkCore;
using PaymentProcessingAPI.Infrastructure;
using PaymentProcessingAPI.Models;
using PaymentProcessingAPI.Models.Entities;
using PaymentProcessingAPI.Services.Interfaces;

namespace PaymentProcessingAPI.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _context;

    public PaymentRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<Payment> CreatePaymentAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(cancellationToken);
        return payment;
    }

    public async Task<Payment?> GetPaymentAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.TransactionId == transactionId, cancellationToken);
    }

    public async Task<Payment> UpdatePaymentAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        payment.UpdatedAt = DateTime.UtcNow;
        _context.Payments.Update(payment);
        await _context.SaveChangesAsync(cancellationToken);
        return payment;
    }

    public async Task<IEnumerable<Payment>> GetPaymentsByFilterAsync(PaymentFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Payments.AsQueryable();

        if (filter.StartDate.HasValue)
            query = query.Where(p => p.CreatedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(p => p.CreatedAt <= filter.EndDate.Value);

        if (filter.Status.HasValue)
            query = query.Where(p => p.Status == filter.Status.Value.ToString());

        if (!string.IsNullOrEmpty(filter.CustomerId))
            query = query.Where(p => p.CustomerId == filter.CustomerId);

        if (filter.PaymentMethod.HasValue)
            query = query.Where(p => p.PaymentMethod == filter.PaymentMethod.Value.ToString());

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetPaymentsCountByFilterAsync(PaymentFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Payments.AsQueryable();

        if (filter.StartDate.HasValue)
            query = query.Where(p => p.CreatedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(p => p.CreatedAt <= filter.EndDate.Value);

        if (filter.Status.HasValue)
            query = query.Where(p => p.Status == filter.Status.Value.ToString());

        if (!string.IsNullOrEmpty(filter.CustomerId))
            query = query.Where(p => p.CustomerId == filter.CustomerId);

        if (filter.PaymentMethod.HasValue)
            query = query.Where(p => p.PaymentMethod == filter.PaymentMethod.Value.ToString());

        return await query.CountAsync(cancellationToken);
    }
}