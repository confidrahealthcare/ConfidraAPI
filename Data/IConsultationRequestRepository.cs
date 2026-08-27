using ConfidraApi.Common.Models;

namespace ConfidraApi.Data;

public interface IConsultationRequestRepository
{
    Task AddAsync(ConsultationRequest request, CancellationToken cancellationToken);
}