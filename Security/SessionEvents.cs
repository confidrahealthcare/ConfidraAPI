using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ConfidraApi.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
namespace ConfidraApi.Security;
public sealed class SessionEvents(ConfidraDbContext db) : CookieAuthenticationEvents
{
    public static string Stamp(string hash) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hash)));
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        if (!int.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) { context.RejectPrincipal(); return; }
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, context.HttpContext.RequestAborted);
        if (user is null || context.Principal?.FindFirstValue("stamp") != Stamp(user.PasswordHash) || context.Principal.FindFirstValue(ClaimTypes.Role) != user.Role || (user.Role != "Patient" && !user.ProfessionalVerified))
        { context.RejectPrincipal(); await context.HttpContext.SignOutAsync(); }
    }
    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context) { context.Response.StatusCode = 401; return Task.CompletedTask; }
    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context) { context.Response.StatusCode = 403; return Task.CompletedTask; }
}
