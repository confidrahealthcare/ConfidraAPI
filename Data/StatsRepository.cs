using Microsoft.EntityFrameworkCore;

namespace ConfidraApi.Data;

public sealed class StatsRepository(ConfidraDbContext dbContext) : IStatsRepository
{
    public async Task<(int PatientsOnboarded, int ProgramFollowUp)> GetAsync(CancellationToken cancellationToken)
    {
        var patientsOnboarded = await dbContext.Users.CountAsync(cancellationToken);
        var programFollowUp = await dbContext.ConsultationRequests.CountAsync(cancellationToken);
        return (patientsOnboarded, programFollowUp);
    }
}