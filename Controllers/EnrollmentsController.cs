using System.Security.Claims;
using ConfidraApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace ConfidraApi.Controllers;
[ApiController,Route("api/enrollments")]
public sealed class EnrollmentsController(ConfidraDbContext db):ControllerBase
{
 [HttpGet] public async Task<IActionResult> Get(CancellationToken ct){int id=int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);return Ok(await db.Enrollments.Where(x=>x.UserId==id).Select(x=>new{x.Id,x.PlanName,x.EnrolledUtc,x.ExpiresUtc}).ToListAsync(ct));}
 [HttpPost] public IActionResult Create()=>StatusCode(410,new{message="Direct enrolment is no longer supported. Payment must be verified by the server."});
}
