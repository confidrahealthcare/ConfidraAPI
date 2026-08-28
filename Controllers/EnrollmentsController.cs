using ConfidraApi.Business;
using ConfidraApi.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace ConfidraApi.Controllers;

[ApiController]
[Route("api/enrollments")]
public sealed class EnrollmentsController(EnrollmentService enrollmentService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEnrollmentRequest request, CancellationToken cancellationToken)
    {
        var result = await enrollmentService.CreateAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(new ProblemDetails { Detail = result.Error });
        }

        return Ok(new { enrollmentId = result.Enrollment!.Id, expiresUtc = result.Enrollment.ExpiresUtc });
    }
}