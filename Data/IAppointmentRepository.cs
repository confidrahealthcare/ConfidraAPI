using ConfidraApi.Common.Models;

namespace ConfidraApi.Data;

public interface IAppointmentRepository
{
    Task AddAsync(Appointment appointment, CancellationToken cancellationToken);
    Task<IReadOnlyList<Appointment>> GetForUserAsync(int userId, CancellationToken cancellationToken);
    Task<Appointment?> FindForUserAsync(int id, int userId, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
}
