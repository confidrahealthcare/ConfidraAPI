using ConfidraApi.Common.Models;

namespace ConfidraApi.Data;

public sealed class ConsultationRequestRepository(ConfidraDbContext dbContext) : IConsultationRequestRepository
{
    public async Task AddAsync(ConsultationRequest request, CancellationToken cancellationToken)
    {
        dbContext.ConsultationRequests.Add(request);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}