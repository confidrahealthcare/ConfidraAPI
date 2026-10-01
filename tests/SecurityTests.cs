using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ConfidraApi.Business;
using ConfidraApi.Common.Models;
using ConfidraApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using Xunit;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

public sealed class CareFactory : WebApplicationFactory<Program>
{
    public bool BrowserPreview {get; init;}
    private readonly SqliteConnection connection = new($"Data Source=care-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        connection.Open(); builder.UseEnvironment("Development"); builder.ConfigureLogging(o=>{o.ClearProviders();o.AddConsole();o.SetMinimumLevel(LogLevel.Warning);});
        builder.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?> { ["ConnectionStrings:ConfidraDb"]="unused-test-only", ["Features:Payments"]="false", ["Features:Email"]="false" }));
        builder.ConfigureServices(services =>
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            if(BrowserPreview) services.PostConfigure<Microsoft.AspNetCore.RateLimiting.RateLimiterOptions>(o=>o.GlobalLimiter=System.Threading.RateLimiting.PartitionedRateLimiter.Create<Microsoft.AspNetCore.Http.HttpContext,string>(_=>System.Threading.RateLimiting.RateLimitPartition.GetNoLimiter("synthetic-browser-preview")));
            services.RemoveAll<DbContextOptions<ConfidraDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.AddDbContext<ConfidraDbContext>(o=>o.UseSqlite(connection.ConnectionString));
        });
    }
    public async Task<HttpClient> Client()
    {
        var client=CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect=false });
        using var scope=Services.CreateScope();await scope.ServiceProvider.GetRequiredService<ConfidraDbContext>().Database.EnsureCreatedAsync();return client;
    }
    public async Task WithDb(Func<ConfidraDbContext,Task> action) {using var scope=Services.CreateScope();await action(scope.ServiceProvider.GetRequiredService<ConfidraDbContext>());}
    protected override void Dispose(bool disposing){base.Dispose(disposing);if(disposing)connection.Dispose();}
}
public sealed class SecurityTests
{
    private const string Password="SyntheticOnly-LongPassword42";
    private static async Task<HttpResponseMessage> Send(HttpClient client,string path,object data,HttpMethod? method=null)
    {
        var token=(await client.GetFromJsonAsync<JsonElement>("/api/auth/csrf")).GetProperty("token").GetString();
        var request=new HttpRequestMessage(method??HttpMethod.Post,path){Content=JsonContent.Create(data)};request.Headers.Add("X-CSRF-TOKEN",token);return await client.SendAsync(request);
    }
    private static async Task<int> RegisterLogin(HttpClient client,string suffix)
    {
        var register=await Send(client,"/api/auth/register",new{fullName="Synthetic Test "+suffix,email=$"test{suffix}@example.invalid",phone="90000000"+suffix,password=Password,adultConfirmed=true});Assert.Equal(HttpStatusCode.Created,register.StatusCode);
        var login=await Send(client,"/api/auth/login",new{emailOrPhone=$"test{suffix}@example.invalid",password=Password});Assert.Equal(HttpStatusCode.OK,login.StatusCode);
        return (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }
    [Fact] public async Task AnonymousEndpointsDenyAccessAndCsrfIsRequired()
    {
        using var f=new CareFactory();using var c=await f.Client();
        foreach(var path in new[]{"/api/care/logs","/api/appointments?userId=1","/api/payments","/api/professional/patients","/api/stats"})Assert.Equal(HttpStatusCode.Unauthorized,(await c.GetAsync(path)).StatusCode);
        var r=await c.PostAsJsonAsync("/api/auth/login",new{emailOrPhone="nobody",password=Password});Assert.Equal(HttpStatusCode.BadRequest,r.StatusCode);
    }
    [Fact] public async Task PatientCannotReadAnotherPatientsDataOrForgeOwnership()
    {
        using var f=new CareFactory();using var a=await f.Client();using var b=await f.Client();var aid=await RegisterLogin(a,"01");var bid=await RegisterLogin(b,"02");
        Assert.Equal(HttpStatusCode.NoContent,(await Send(a,"/api/care/consents",new{purpose="HealthData",granted=true})).StatusCode);
        var day=DateOnly.FromDateTime(DateTime.UtcNow);
        Assert.Equal(HttpStatusCode.NoContent,(await Send(a,"/api/care/logs",new{date=day,fastingGlucose=101,energy=7,userId=bid},HttpMethod.Put)).StatusCode);
        var other=await b.GetFromJsonAsync<JsonElement>($"/api/care/logs?userId={aid}");Assert.Equal(0,other.GetArrayLength());
        var own=await a.GetFromJsonAsync<JsonElement>("/api/care/logs");Assert.Equal(1,own.GetArrayLength());
        Assert.Equal(HttpStatusCode.Forbidden,(await b.GetAsync("/api/professional/patients")).StatusCode);
    }
    [Fact] public async Task ConsentWithdrawalStopsCollectionAndLogValidationRejectsBadReadings()
    {
        using var f=new CareFactory();using var c=await f.Client();await RegisterLogin(c,"03");var log=new{date=DateOnly.FromDateTime(DateTime.UtcNow),energy=7};
        Assert.Equal(HttpStatusCode.Forbidden,(await Send(c,"/api/care/logs",log,HttpMethod.Put)).StatusCode);
        await Send(c,"/api/care/consents",new{purpose="HealthData",granted=true});
        Assert.Equal(HttpStatusCode.BadRequest,(await Send(c,"/api/care/logs",new{date=log.date,energy=11},HttpMethod.Put)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await Send(c,"/api/care/logs",new{date=log.date,systolic=80,diastolic=120},HttpMethod.Put)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,(await Send(c,"/api/care/logs",log,HttpMethod.Put)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,(await Send(c,"/api/care/logs",log,HttpMethod.Put)).StatusCode);
        Assert.Equal(1,(await c.GetFromJsonAsync<JsonElement>("/api/care/logs")).GetArrayLength());
        await Send(c,"/api/care/consents",new{purpose="HealthData",granted=false});
        Assert.Equal(HttpStatusCode.Forbidden,(await Send(c,"/api/care/logs",log,HttpMethod.Put)).StatusCode);
    }
    [Fact] public async Task PaymentReferenceAndFakeSlotsCannotCreateEnrollment()
    {
        using var f=new CareFactory();using var c=await f.Client();await RegisterLogin(c,"04");
        Assert.Equal(HttpStatusCode.Gone,(await Send(c,"/api/enrollments",new{paymentId="made-up",planName="anything",userId=1})).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await Send(c,"/api/appointments",new{doctorName="Invented",appointmentTime="09:00"})).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable,(await Send(c,"/api/payments/orders",new{programmeId="diabetes-90",amount=1})).StatusCode);
        Assert.Equal(0,(await c.GetFromJsonAsync<JsonElement>("/api/enrollments")).GetArrayLength());
    }
    [Fact] public async Task PasswordChangeRevokesExistingCookie()
    {
        using var f=new CareFactory();using var c=await f.Client();var id=await RegisterLogin(c,"05");
        await f.WithDb(async db=>{var u=await db.Users.FindAsync(id);u!.PasswordHash="changed-hash";await db.SaveChangesAsync();});
        Assert.Equal(HttpStatusCode.Unauthorized,(await c.GetAsync("/api/auth/me")).StatusCode);
    }
    [Fact] public async Task GuideCannotWriteClinicalReviewAndUnverifiedRoleCannotSignIn()
    {
        using var f=new CareFactory();using var c=await f.Client();var id=await RegisterLogin(c,"06");
        await f.WithDb(async db=>{var u=await db.Users.FindAsync(id);u!.Role="Guide";await db.SaveChangesAsync();});
        Assert.Equal(HttpStatusCode.Forbidden,(await Send(c,"/api/auth/login",new{emailOrPhone="test06@example.invalid",password=Password})).StatusCode);
        await f.WithDb(async db=>{var u=await db.Users.FindAsync(id);u!.ProfessionalVerified=true;await db.SaveChangesAsync();});
        Assert.Equal(HttpStatusCode.OK,(await Send(c,"/api/auth/login",new{emailOrPhone="test06@example.invalid",password=Password})).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await Send(c,"/api/professional/patients/1/reviews",new{day=0,interpretation="unauthorized"})).StatusCode);
    }
    [Fact] public async Task RegistrationRequiresAdultConfirmationAndCannotEscalateRole()
    {
        using var f=new CareFactory();using var c=await f.Client();
        Assert.Equal(HttpStatusCode.BadRequest,(await Send(c,"/api/auth/register",new{fullName="Synthetic",email="adult@example.invalid",phone="9000000044",password=Password,adultConfirmed=false})).StatusCode);
        await RegisterLogin(c,"07");Assert.Equal("Patient",(await c.GetFromJsonAsync<JsonElement>("/api/auth/me")).GetProperty("role").GetString());
        Assert.Equal(HttpStatusCode.ServiceUnavailable,(await Send(c,"/api/auth/password-reset/request",new{email="test07@example.invalid"})).StatusCode);
    }
    [Fact] public async Task ExpiredResetCodeCannotChangePasswordAndValidCodeIsSingleUse()
    {
        using var f=new CareFactory();using var c=await f.Client();var id=await RegisterLogin(c,"08");
        await f.WithDb(async db=>{var u=(await db.Users.FindAsync(id))!;u.PasswordResetOtpHash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("123456")));u.PasswordResetOtpExpiresUtc=DateTime.UtcNow.AddMinutes(-1);await db.SaveChangesAsync();});
        var reset=new{email="test08@example.invalid",otp="123456",newPassword="NewSynthetic-Password42"};
        Assert.Equal(HttpStatusCode.BadRequest,(await Send(c,"/api/auth/password-reset/complete",reset)).StatusCode);
        await f.WithDb(async db=>{var u=(await db.Users.FindAsync(id))!;u.PasswordResetOtpExpiresUtc=DateTime.UtcNow.AddMinutes(5);await db.SaveChangesAsync();});
        Assert.Equal(HttpStatusCode.OK,(await Send(c,"/api/auth/password-reset/complete",reset)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await c.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await Send(c,"/api/auth/password-reset/complete",reset)).StatusCode);
    }
    [Fact] public async Task PhysicianNeedsAssignmentAndSharingAndReferralNeedsPatientPermission()
    {
        using var f=new CareFactory();using var patient=await f.Client();using var physician=await f.Client();var pid=await RegisterLogin(patient,"09");var doctor=await RegisterLogin(physician,"10");
        await f.WithDb(async db=>{var u=(await db.Users.FindAsync(doctor))!;u.Role="Physician";u.ProfessionalVerified=true;await db.SaveChangesAsync();});
        await Send(physician,"/api/auth/login",new{emailOrPhone="test10@example.invalid",password=Password});
        Assert.Equal(HttpStatusCode.NotFound,(await physician.GetAsync($"/api/professional/patients/{pid}")).StatusCode);
        await f.WithDb(async db=>{db.CareAssignments.Add(new CareAssignment{PatientId=pid,StaffId=doctor});await db.SaveChangesAsync();});
        Assert.Equal(HttpStatusCode.NotFound,(await physician.GetAsync($"/api/professional/patients/{pid}")).StatusCode);
        await Send(patient,"/api/care/consents",new{purpose="ClinicalSharing",granted=true});await Send(patient,"/api/care/consents",new{purpose="HealthData",granted=true});
        Assert.Equal(HttpStatusCode.OK,(await physician.GetAsync($"/api/professional/patients/{pid}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await Send(physician,"/api/professional/referrals",new{patientName="Synthetic",phone="9000000055",patientConsent=false})).StatusCode);
        Assert.Equal(HttpStatusCode.Created,(await Send(physician,"/api/professional/referrals",new{patientName="Synthetic",phone="9000000055",patientConsent=true})).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await Send(physician,$"/api/professional/patients/{pid}/enrollments",new{orderId="invented",day0=DateOnly.FromDateTime(DateTime.UtcNow)})).StatusCode);
    }
    [Fact] public async Task ClinicalActivationRequiresAssessmentAndIsIdempotent()
    {
        using var f=new CareFactory();using var patient=await f.Client();using var physician=await f.Client();var pid=await RegisterLogin(patient,"11");var doctor=await RegisterLogin(physician,"12");
        await f.WithDb(async db=>{var u=(await db.Users.FindAsync(doctor))!;u.Role="Physician";u.ProfessionalVerified=true;db.CareAssignments.Add(new CareAssignment{PatientId=pid,StaffId=doctor});db.PaymentRecords.Add(new PaymentRecord{OrderId="order_activation",UserId=pid,ProgrammeId="pre-diabetes-90",AmountPaise=749900,PaymentId="pay_activation",Status="CapturedPendingAssessment",CreatedUtc=DateTime.UtcNow});await db.SaveChangesAsync();});
        await Send(physician,"/api/auth/login",new{emailOrPhone="test12@example.invalid",password=Password});
        foreach(var purpose in new[]{"ClinicalSharing","HealthData"})await Send(patient,"/api/care/consents",new{purpose,granted=true});
        var input=new{orderId="order_activation",day0=DateOnly.FromDateTime(DateTime.UtcNow.AddMinutes(330))};
        Assert.Equal(HttpStatusCode.Conflict,(await Send(physician,$"/api/professional/patients/{pid}/enrollments",input)).StatusCode);
        Assert.Equal(HttpStatusCode.Created,(await Send(physician,$"/api/professional/patients/{pid}/reviews",new{day=0,interpretation="Synthetic assessment only"})).StatusCode);
        Assert.Equal(HttpStatusCode.Created,(await Send(physician,$"/api/professional/patients/{pid}/enrollments",input)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await Send(physician,$"/api/professional/patients/{pid}/enrollments",input)).StatusCode);
        Assert.Equal(1,(await patient.GetFromJsonAsync<JsonElement>("/api/enrollments")).GetArrayLength());
        await Send(patient,"/api/care/consents",new{purpose="HealthData",granted=false});Assert.Equal(HttpStatusCode.NotFound,(await physician.GetAsync($"/api/professional/patients/{pid}")).StatusCode);
    }
    [Fact] public void PaymentSignaturesAndCatalogueUseServerValues()
    {
        const string secret="synthetic-test-only-secret";const string payload="order_test|pay_test";
        var signature=Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret),Encoding.UTF8.GetBytes(payload)));
        Assert.True(RazorpayService.ValidSignature(payload,signature,secret));Assert.False(RazorpayService.ValidSignature(payload+"x",signature,secret));Assert.False(RazorpayService.ValidSignature(payload,new string('z',64),secret));Assert.False(RazorpayService.ValidSignature(payload,signature,""));Assert.Equal(749900,RazorpayService.Prices["pre-diabetes-90"]);Assert.Equal(1699900,RazorpayService.Prices["diabetes-90"]);
    }
}
