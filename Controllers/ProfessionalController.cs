using System.Security.Claims;
using ConfidraApi.Common.Models;
using ConfidraApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace ConfidraApi.Controllers;
[ApiController,Route("api/professional")]
public sealed class ProfessionalController(ConfidraDbContext db):ControllerBase
{
 private int Id=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 private Task<bool> Assigned(int p,CancellationToken ct)=>db.CareAssignments.AnyAsync(x=>x.PatientId==p&&x.StaffId==Id,ct);
 private async Task<bool> Sharing(int p,CancellationToken ct)=>await db.CareConsents.CountAsync(x=>x.UserId==p&&(x.Purpose=="ClinicalSharing"||x.Purpose=="HealthData")&&x.WithdrawnUtc==null,ct)==2;
 private void Audit(string a,int? p=null)=>db.AuditEvents.Add(new AuditEvent{ActorId=Id,SubjectId=p,Action=a,CreatedUtc=DateTime.UtcNow});
 [Authorize(Roles="Physician,Guide"),HttpGet("patients")] public async Task<IActionResult> Patients(CancellationToken ct){var rows=await(from a in db.CareAssignments join u in db.Users on a.PatientId equals u.Id where a.StaffId==Id select new{u.Id,u.FullName}).ToListAsync(ct);Audit("AssignedPatientsViewed");await db.SaveChangesAsync(ct);return Ok(rows);}
 [Authorize(Roles="Physician"),HttpGet("patients/{patientId:int}")] public async Task<IActionResult> Patient(int patientId,CancellationToken ct){if(!await Assigned(patientId,ct)||!await Sharing(patientId,ct))return NotFound();var intake=await db.PatientIntakes.AsNoTracking().SingleOrDefaultAsync(x=>x.UserId==patientId,ct);var logs=await db.DailyLogs.Where(x=>x.UserId==patientId).OrderByDescending(x=>x.Date).Take(90).ToListAsync(ct);var reviews=await db.ClinicalReviews.Where(x=>x.PatientId==patientId).ToListAsync(ct);Audit("ClinicalRecordViewed",patientId);await db.SaveChangesAsync(ct);return Ok(new{intake,logs,reviews});}
 [Authorize(Roles="Guide"),HttpGet("patients/{patientId:int}/adherence")] public async Task<IActionResult> Adherence(int patientId,CancellationToken ct){if(!await Assigned(patientId,ct)||!await Sharing(patientId,ct))return NotFound();var dates=await db.DailyLogs.Where(x=>x.UserId==patientId).OrderByDescending(x=>x.Date).Select(x=>x.Date).Take(90).ToListAsync(ct);Audit("AdherenceDatesViewed",patientId);await db.SaveChangesAsync(ct);return Ok(new{dates});}
 [Authorize(Roles="Physician"),HttpPost("patients/{patientId:int}/reviews")] public async Task<IActionResult> Review(int patientId,ReviewInput input,CancellationToken ct){if(!await Assigned(patientId,ct)||!await Sharing(patientId,ct))return NotFound();if(input.Day is not(0 or 30 or 60 or 90))return BadRequest();db.ClinicalReviews.Add(new ClinicalReview{PatientId=patientId,PhysicianId=Id,Day=input.Day,Fbs=input.Fbs,Ppbs=input.Ppbs,HbA1c=input.HbA1c,Interpretation=input.Interpretation.Trim(),RecordedUtc=DateTime.UtcNow});Audit("ClinicalReviewRecorded",patientId);await db.SaveChangesAsync(ct);return StatusCode(201);}
 [Authorize(Roles="Physician,Referrer"),HttpGet("referrals")] public async Task<IActionResult> Referrals(CancellationToken ct)=>Ok(await db.Referrals.Where(x=>x.ReferrerId==Id).Select(x=>new{x.Id,x.PatientName,x.Status,x.CreatedUtc}).ToListAsync(ct));
 [Authorize(Roles="Physician,Referrer"),HttpPost("referrals")] public async Task<IActionResult> Refer(ReferralInput input,CancellationToken ct){if(!input.PatientConsent)return BadRequest(new{message="Patient permission is required."});var row=new Referral{ReferrerId=Id,PatientName=input.PatientName.Trim(),Phone=input.Phone.Trim(),ConsentConfirmedUtc=DateTime.UtcNow,CreatedUtc=DateTime.UtcNow};db.Referrals.Add(row);Audit("ReferralSubmitted");await db.SaveChangesAsync(ct);return StatusCode(201,new{row.Id,row.Status});}
 [Authorize(Roles="Operations"),HttpGet("referral-queue")] public async Task<IActionResult> Queue(CancellationToken ct){var rows=await db.Referrals.OrderByDescending(x=>x.CreatedUtc).Take(100).Select(x=>new{x.Id,x.PatientName,x.Phone,x.Status,x.CreatedUtc}).ToListAsync(ct);Audit("ReferralQueueViewed");await db.SaveChangesAsync(ct);return Ok(rows);}
 [Authorize(Roles="Operations"),HttpPatch("referrals/{id:int}/status")] public async Task<IActionResult> Status(int id,[FromBody]string status,CancellationToken ct){if(status is not("Contacted" or "Assessment arranged" or "Closed"))return BadRequest();var row=await db.Referrals.FindAsync([id],ct);if(row is null)return NotFound();row.Status=status;Audit("ReferralStatusUpdated");await db.SaveChangesAsync(ct);return NoContent();}
}
