using ConfidraApi.Business;
using ConfidraApi.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace ConfidraApi.Controllers;

[ApiController]
[Route("api/appointments")]
public sealed class AppointmentsController(AppointmentService appointmentService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<Appointment>> GetForUser([FromQuery] int userId, CancellationToken cancellationToken) =>
        appointmentService.GetForUserAsync(userId, cancellationToken);

    [HttpPost("{appointmentId:int}/cancel")]
    public async Task<IActionResult> Cancel(int appointmentId, [FromQuery] int userId, CancellationToken cancellationToken)
    {
        var result = await appointmentService.CancelAsync(appointmentId, userId, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(new ProblemDetails { Detail = result.Error });
        }

        return Ok(new { message = result.Error });
    }

    [HttpPost]
    public async Task<IActionResult> Book(
        [FromBody] BookAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await appointmentService.BookAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(new ProblemDetails { Detail = result.Error });
        }

        return Ok(new { message = result.Error });
    }
}