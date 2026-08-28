using ConfidraApi.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace ConfidraApi.Data;

public sealed class EnrollmentRepository(ConfidraDbContext dbContext) : IEnrollmentRepository
{
    public async Task AddAsync(Enrollment enrollment, CancellationToken cancellationToken)
    {
        dbContext.Enrollments.Add(enrollment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Enrollment?> FindAsync(int id, CancellationToken cancellationToken) =>
        dbContext.Enrollments.SingleOrDefaultAsync(enrollment => enrollment.Id == id, cancellationToken);

    public Task<Enrollment?> FindByPaymentIdAsync(string paymentId, CancellationToken cancellationToken) =>
        dbContext.Enrollments.SingleOrDefaultAsync(enrollment => enrollment.PaymentId == paymentId, cancellationToken);
}