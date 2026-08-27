using ConfidraApi.Business;
using ConfidraApi.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace ConfidraApi.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PaymentsController(RazorpayService razorpayService) : ControllerBase
{
    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreatePaymentOrderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await razorpayService.CreateOrderAsync(request, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Detail = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails { Detail = exception.Message });
        }
    }
}
