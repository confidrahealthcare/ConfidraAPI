using System.Security.Claims;
using ConfidraApi.Business;
using ConfidraApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace ConfidraApi.Controllers;
[ApiController,Route("api/payments")]
public sealed class PaymentsController(RazorpayService gateway,ConfidraDbContext db):ControllerBase
{
 private int Id=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 [HttpGet] public async Task<IActionResult> Get(CancellationToken ct)=>Ok(await db.PaymentRecords.Where(x=>x.UserId==Id).Select(x=>new{x.OrderId,x.AmountPaise,x.Status,x.CreatedUtc}).ToListAsync(ct));
 [HttpGet("availability")] public IActionResult Availability()=>Ok(new{enabled=gateway.CheckoutEnabled,mode="test",termsUrl=gateway.CheckoutEnabled?gateway.TermsUrl:null,programmes=RazorpayService.Prices.Select(p=>new{id=p.Key,name=p.Key=="pre-diabetes-90"?"Pre-Diabetes 90":"Type 1 / Type 2 Diabetes 90",amountPaise=p.Value})});
 [Authorize(Roles="Patient"),HttpPost("orders")] public async Task<IActionResult> Create(OrderInput input,CancellationToken ct)
 {if(!gateway.CheckoutEnabled)return StatusCode(503,new{message="Online payment is unavailable. Confirm terms with the care team."});if(!input.TermsAccepted)return BadRequest(new{message="Read and accept the approved purchase terms before continuing."});try{return Ok(await gateway.CreateOrderAsync(input.ProgrammeId,Id,ct));}catch(ArgumentException){return BadRequest(new{message="Invalid programme."});}catch(HttpRequestException){return StatusCode(503,new{message="Payment provider unavailable. No enrolment has been created."});}}
 [Authorize(Roles="Patient"),HttpPost("verify")] public async Task<IActionResult> Verify(VerifyPaymentInput input,CancellationToken ct)
 {if(!gateway.Enabled)return StatusCode(503);try{return await gateway.VerifyAsync(input,Id,ct)?Ok(new{message="Payment captured. Clinical assessment is still required."}):BadRequest(new{message="Payment could not be verified."});}catch(HttpRequestException){return StatusCode(503,new{message="Verification is pending. Do not pay again; contact the care team."});}}
 [AllowAnonymous,IgnoreAntiforgeryToken,HttpPost("webhook"),RequestSizeLimit(65536)] public async Task<IActionResult> Webhook(CancellationToken ct)
 {using var reader=new StreamReader(Request.Body);var raw=await reader.ReadToEndAsync(ct);try{return await gateway.WebhookAsync(raw,Request.Headers["X-Razorpay-Signature"].ToString(),Request.Headers["X-Razorpay-Event-Id"].ToString(),ct)?Ok():BadRequest();}catch(System.Text.Json.JsonException){return BadRequest();}catch(KeyNotFoundException){return BadRequest();}catch(HttpRequestException){return StatusCode(503);}}
}
