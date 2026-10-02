using System.Security.Claims;
using ConfidraApi.Business;
using ConfidraApi.Common.Models;
using ConfidraApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
namespace ConfidraApi.Controllers;
public sealed record ActivationInput([Required,MaxLength(80)]string OrderId,DateOnly Day0);
[ApiController,Route("api/professional/patients/{patientId:int}/enrollments"),Authorize(Roles="Physician")]
public sealed class ActivationController(ConfidraDbContext db):ControllerBase
{
 [HttpGet] public async Task<IActionResult> Eligible(int patientId,CancellationToken ct)
 {
  int id=int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
  if(!await db.CareAssignments.AnyAsync(x=>x.PatientId==patientId&&x.StaffId==id,ct))return NotFound();
  if(await db.CareConsents.CountAsync(x=>x.UserId==patientId&&(x.Purpose=="HealthData"||x.Purpose=="ClinicalSharing")&&x.WithdrawnUtc==null,ct)!=2)return NotFound();
  var orders=await db.PaymentRecords.Where(x=>x.UserId==patientId&&x.PaymentId!=null&&(x.Status=="CapturedPendingAssessment"||x.Status=="Active")).Select(x=>new{x.OrderId,x.Status,programmeName=x.ProgrammeId=="pre-diabetes-90"?"Pre-Diabetes 90":"Type 1 / Type 2 Diabetes 90"}).ToListAsync(ct);
  db.AuditEvents.Add(new AuditEvent{ActorId=id,SubjectId=patientId,Action="EnrollmentEligibilityViewed",CreatedUtc=DateTime.UtcNow});await db.SaveChangesAsync(ct);return Ok(orders);
 }
 [HttpPost] public async Task<IActionResult> Activate(int patientId,ActivationInput input,CancellationToken ct)
 {
  int id=int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
  if(!await db.CareAssignments.AnyAsync(x=>x.PatientId==patientId&&x.StaffId==id,ct))return NotFound();
  var purposes=await db.CareConsents.Where(x=>x.UserId==patientId&&x.WithdrawnUtc==null).Select(x=>x.Purpose).ToListAsync(ct);
  if(!purposes.Contains("HealthData")||!purposes.Contains("ClinicalSharing"))return Conflict(new{message="Patient consent is required."});
  if(!await db.ClinicalReviews.AnyAsync(x=>x.PatientId==patientId&&x.PhysicianId==id&&x.Day==0,ct))return Conflict(new{message="Record your Day 0 clinical assessment first."});
  var today=DateOnly.FromDateTime(DateTime.UtcNow.AddMinutes(330));if(input.Day0>today||input.Day0<today.AddDays(-30))return BadRequest(new{message="Confirm a recent Day 0 date."});
  await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable,ct);
  var order=await db.PaymentRecords.SingleOrDefaultAsync(x=>x.OrderId==input.OrderId&&x.UserId==patientId,ct);
  if(order is null||order.PaymentId is null||order.Status is not("CapturedPendingAssessment" or "Active")||!RazorpayService.Prices.ContainsKey(order.ProgrammeId))return Conflict(new{message="A verified captured payment for this patient is required."});
  var existing=await db.Enrollments.SingleOrDefaultAsync(x=>x.PaymentId==order.PaymentId,ct);if(existing is not null)return existing.UserId==patientId?Ok(new{existing.Id}):Conflict();
  var start=DateTime.SpecifyKind(input.Day0.ToDateTime(TimeOnly.MinValue).AddMinutes(-330),DateTimeKind.Utc);
  var row=new Enrollment{UserId=patientId,PaymentId=order.PaymentId,PlanName=order.ProgrammeId=="pre-diabetes-90"?"Pre-Diabetes 90":"Type 1 / Type 2 Diabetes 90",EnrolledUtc=start,ExpiresUtc=start.AddDays(90)};
  db.Enrollments.Add(row);order.Status="Active";db.AuditEvents.Add(new AuditEvent{ActorId=id,SubjectId=patientId,Action="ClinicalEnrollmentApproved",CreatedUtc=DateTime.UtcNow});await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return StatusCode(201,new{row.Id});
 }
}
