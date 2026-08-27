namespace ConfidraApi.Data;

public interface IStatsRepository
{
    Task<(int PatientsOnboarded, int ProgramFollowUp)> GetAsync(CancellationToken cancellationToken);
}