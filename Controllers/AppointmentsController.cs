using System.Security.Claims;
using ConfidraApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace ConfidraApi.Controllers;
[ApiController,Route("api/appointments")]
public sealed class AppointmentsController(ConfidraDbContext db):ControllerBase
{
 [HttpGet] public async Task<IActionResult> Get(CancellationToken ct){int id=int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);return Ok(await db.Appointments.Where(x=>x.UserId==id).Select(x=>new{x.Id,x.AppointmentDate,x.AppointmentTime,x.DoctorName,x.Status}).ToListAsync(ct));}
 [HttpPost] public IActionResult Book()=>StatusCode(409,new{message="Use the care team's scheduling service. App booking awaits calendar synchronization."});
 [HttpPost("{id:int}/cancel")] public IActionResult Cancel(int id)=>StatusCode(409,new{message="Use your scheduling confirmation or contact the care team to cancel."});
}
