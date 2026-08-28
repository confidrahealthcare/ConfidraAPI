using ConfidraApi.Common.Models;

namespace ConfidraApi.Data;

public interface IEnrollmentRepository
{
    Task AddAsync(Enrollment enrollment, CancellationToken cancellationToken);
    Task<Enrollment?> FindAsync(int id, CancellationToken cancellationToken);
    Task<Enrollment?> FindByPaymentIdAsync(string paymentId, CancellationToken cancellationToken);
}