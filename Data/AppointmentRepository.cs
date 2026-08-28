using ConfidraApi.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace ConfidraApi.Data;

public sealed class AppointmentRepository(ConfidraDbContext dbContext) : IAppointmentRepository
{
    public async Task AddAsync(Appointment appointment, CancellationToken cancellationToken)
    {
        dbContext.Appointments.Add(appointment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetForUserAsync(int userId, CancellationToken cancellationToken)
    {
        return await dbContext.Appointments
            .AsNoTracking()
            .Where(appointment => appointment.UserId == userId)
            .OrderBy(appointment => appointment.AppointmentDate)
            .ThenBy(appointment => appointment.AppointmentTime)
            .ToListAsync(cancellationToken);
    }

    public Task<Appointment?> FindForUserAsync(int id, int userId, CancellationToken cancellationToken) =>
        dbContext.Appointments.SingleOrDefaultAsync(appointment => appointment.Id == id && appointment.UserId == userId, cancellationToken);

    public Task SaveAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
