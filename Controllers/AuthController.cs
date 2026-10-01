using System.Security.Claims;
using ConfidraApi.Business;
using ConfidraApi.Common.Models;
using ConfidraApi.Data;
using ConfidraApi.Security;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace ConfidraApi.Controllers;
[ApiController, Route("api/auth")]
public sealed class AuthController(AuthService auth, ConfidraDbContext db, IAntiforgery antiforgery, IConfiguration config) : ControllerBase
{
    [AllowAnonymous, HttpGet("csrf")] public IActionResult Csrf() => Ok(new { token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });
    [AllowAnonymous, HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest input, CancellationToken ct) { var r=await auth.RegisterAsync(input,ct); if(!r.Succeeded) return BadRequest(new { message="Registration could not be completed. Check your details or sign in." }); return StatusCode(201,new { message="Account created. Sign in to continue." }); }
    [AllowAnonymous, HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest input, CancellationToken ct)
    {
        var r=await auth.LoginAsync(input,ct); if(!r.Succeeded) return Unauthorized(new { message="Sign-in unsuccessful. Check your details or try again later." });
        var user=await db.Users.SingleAsync(x=>x.Id==r.User!.Id,ct); if(user.Role!="Patient" && !user.ProfessionalVerified) return Forbid();
        var claims=new[]{new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),new Claim(ClaimTypes.Role,user.Role),new Claim("stamp",SessionEvents.Stamp(user.PasswordHash))};
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme)));
        db.AuditEvents.Add(new AuditEvent {ActorId=user.Id,Action="SessionCreated",CreatedUtc=DateTime.UtcNow}); await db.SaveChangesAsync(ct); return Ok(new {user.Id,user.FullName,user.Role});
    }
    [HttpGet("me")] public async Task<IActionResult> Me(CancellationToken ct) { int id=int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!); return Ok(await db.Users.Where(x=>x.Id==id).Select(x=>new{x.Id,x.FullName,x.Email,x.Phone,x.Role}).SingleAsync(ct)); }
    [HttpPost("logout")] public async Task<IActionResult> Logout() {await HttpContext.SignOutAsync();return NoContent();}
    [AllowAnonymous,HttpPost("password-reset/request")] public async Task<IActionResult> RequestReset(PasswordResetRequest input,CancellationToken ct) {if(!config.GetValue<bool>("Features:Email"))return StatusCode(503,new{message="Password reset is unavailable. Contact care support."});await auth.RequestPasswordResetAsync(input,ct);return Accepted(new{message="If an eligible account exists, a reset code will be sent."});}
    [AllowAnonymous,HttpPost("password-reset/complete")] public async Task<IActionResult> Reset(ResetPasswordRequest input,CancellationToken ct) {var r=await auth.ResetPasswordAsync(input,ct);return r.Succeeded?Ok(new{message="Password updated. Sign in again."}):BadRequest(new{message="The code is invalid or expired, or the password does not meet requirements."});}
}
