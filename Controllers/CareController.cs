using System.Security.Claims;
using ConfidraApi.Common.Models;
using ConfidraApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace ConfidraApi.Controllers;
[ApiController,Route("api/care"),Authorize(Roles="Patient")]
public sealed class CareController(ConfidraDbContext db):ControllerBase
{
 private int Id=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 private Task<bool> Consented(CancellationToken ct)=>db.CareConsents.AnyAsync(x=>x.UserId==Id&&x.Purpose=="HealthData"&&x.WithdrawnUtc==null,ct);
 private void Audit(string action)=>db.AuditEvents.Add(new AuditEvent{ActorId=Id,SubjectId=Id,Action=action,CreatedUtc=DateTime.UtcNow});
 [HttpGet("consents")] public async Task<IActionResult> Consents(CancellationToken ct)=>Ok(await db.CareConsents.Where(x=>x.UserId==Id).Select(x=>new{x.Purpose,x.Version,x.GrantedUtc,x.WithdrawnUtc}).ToListAsync(ct));
 [HttpPost("consents")] public async Task<IActionResult> Consent(ConsentInput input,CancellationToken ct){var active=await db.CareConsents.Where(x=>x.UserId==Id&&x.Purpose==input.Purpose&&x.WithdrawnUtc==null).ToListAsync(ct);if(input.Granted&&active.Count==0)db.CareConsents.Add(new CareConsent{UserId=Id,Purpose=input.Purpose,GrantedUtc=DateTime.UtcNow});if(!input.Granted)foreach(var c in active)c.WithdrawnUtc=DateTime.UtcNow;Audit(input.Granted?"ConsentGranted":"ConsentWithdrawn");await db.SaveChangesAsync(ct);return NoContent();}
 [HttpGet("intake")] public async Task<IActionResult> Intake(CancellationToken ct){var row=await db.PatientIntakes.AsNoTracking().SingleOrDefaultAsync(x=>x.UserId==Id,ct);Audit("IntakeViewed");await db.SaveChangesAsync(ct);return Ok(row);}
 [HttpPut("intake")] public async Task<IActionResult> SaveIntake(IntakeInput input,CancellationToken ct){if(!await Consented(ct))return StatusCode(403,new{message="Health-data consent is required."});var row=await db.PatientIntakes.FindAsync([Id],ct);if(row is null){row=new PatientIntake{UserId=Id};db.PatientIntakes.Add(row);}row.City=input.City.Trim();row.DiabetesType=input.DiabetesType;row.Duration=input.Duration.Trim();row.Medicines=input.Medicines.Trim();row.OtherConditions=input.OtherConditions.Trim();row.UpdatedUtc=DateTime.UtcNow;Audit("IntakeUpdated");await db.SaveChangesAsync(ct);return NoContent();}
 [HttpGet("logs")] public async Task<IActionResult> Logs(CancellationToken ct){var rows=await db.DailyLogs.Where(x=>x.UserId==Id).OrderByDescending(x=>x.Date).Take(366).ToListAsync(ct);Audit("LogsViewed");await db.SaveChangesAsync(ct);return Ok(rows);}
 [HttpPut("logs")] public async Task<IActionResult> SaveLog(LogInput input,CancellationToken ct)
 {
  if(!await Consented(ct))return StatusCode(403,new{message="Health-data consent is required."});var today=DateOnly.FromDateTime(DateTime.UtcNow.AddMinutes(330));
  if(input.Date>today||input.Date<today.AddDays(-365)||(input.Systolic.HasValue!=input.Diastolic.HasValue)||(input.Systolic.HasValue&&input.Systolic<=input.Diastolic)||(input.FastingGlucose is null&&input.Systolic is null&&input.WeightKg is null&&input.Energy is null))return BadRequest(new{message="Enter a valid date and at least one measurement. Blood pressure requires both readings."});
  var row=await db.DailyLogs.SingleOrDefaultAsync(x=>x.UserId==Id&&x.Date==input.Date,ct);if(row is null){row=new DailyLog{UserId=Id,Date=input.Date};db.DailyLogs.Add(row);}row.FastingGlucose=input.FastingGlucose;row.Systolic=input.Systolic;row.Diastolic=input.Diastolic;row.WeightKg=input.WeightKg;row.Energy=input.Energy;row.UpdatedUtc=DateTime.UtcNow;Audit("DailyLogUpdated");await db.SaveChangesAsync(ct);return NoContent();
 }
 [HttpGet("report")] public async Task<IActionResult> Report(CancellationToken ct){var reviews=await db.ClinicalReviews.Where(x=>x.PatientId==Id).OrderBy(x=>x.RecordedUtc).ToListAsync(ct);Audit("ReportViewed");await db.SaveChangesAsync(ct);return Ok(new{reviews,message="Clinical interpretation is supplied by your physician. No automated medical interpretation."});}
}
