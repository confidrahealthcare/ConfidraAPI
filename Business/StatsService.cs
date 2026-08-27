using ConfidraApi.Common.Models;
using ConfidraApi.Data;

namespace ConfidraApi.Business;

public sealed class StatsService(IStatsRepository repository)
{
    public async Task<DashboardStatsResponse> GetAsync(CancellationToken cancellationToken)
    {
        var stats = await repository.GetAsync(cancellationToken);
        return new DashboardStatsResponse(stats.PatientsOnboarded, stats.ProgramFollowUp, 0, 0);
    }
}