using ConfidraApi.Business;
using ConfidraApi.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace ConfidraApi.Controllers;

[ApiController]
[Route("api/stats")]
public sealed class StatsController(StatsService statsService) : ControllerBase
{
    [HttpGet]
    public Task<DashboardStatsResponse> Get(CancellationToken cancellationToken) =>
        statsService.GetAsync(cancellationToken);
}