using ConfidraApi.Business;
using ConfidraApi.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace ConfidraApi.Controllers;

[ApiController]
[Route("api/consultations")]
public sealed class ConsultationController(ConsultationService consultationService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] ConsultationRequestInput input,
        CancellationToken cancellationToken)
    {
        var result = await consultationService.CreateAsync(input, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(new ProblemDetails { Detail = result.Error });
        }

        return Ok(new { message = "Thank you. Our clinical pharmacist will contact you soon." });
    }
}