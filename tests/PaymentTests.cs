using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using ConfidraApi.Business;
using ConfidraApi.Common.Models;
using ConfidraApi.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;
public sealed class PaymentTests
{
 [Fact] public void CheckoutRequiresExplicitTermsApprovalAndHttpsTerms()
 {
  using var db=new ConfidraDbContext(new DbContextOptionsBuilder<ConfidraDbContext>().UseSqlite("Data Source=:memory:").Options);
  var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{["Features:Payments"]="true",["Razorpay:KeyId"]="rzp_test_synthetic",["Razorpay:KeySecret"]="synthetic-only"}).Build();
  var service=new RazorpayService(config,new HttpClient(new NoNetwork()),db);
  Assert.True(service.Enabled);Assert.False(service.CheckoutEnabled);
  config["Payments:TermsApproved"]="true";config["Payments:TermsUrl"]="http://example.invalid/terms";Assert.False(service.CheckoutEnabled);
  config["Payments:TermsUrl"]="https://example.invalid/terms";Assert.True(service.CheckoutEnabled);
  config["Razorpay:KeyId"]="rzp_live_synthetic";Assert.False(service.CheckoutEnabled);
 }
 [Fact] public async Task RefundReconciliationUsesProviderTotalAndIsIdempotent()
 {
  using var connection=new SqliteConnection("Data Source=:memory:");await connection.OpenAsync();
  await using var db=new ConfidraDbContext(new DbContextOptionsBuilder<ConfidraDbContext>().UseSqlite(connection).Options);await db.Database.EnsureCreatedAsync();
  db.Users.Add(new User{Id=1,FullName="Synthetic",Email="refund@example.invalid",Phone="9000000042",PasswordHash="test-only",CreatedUtc=DateTime.UtcNow});await db.SaveChangesAsync();
  db.PaymentRecords.Add(new PaymentRecord{OrderId="order_refund",PaymentId="pay_refund",UserId=1,ProgrammeId="pre-diabetes-90",AmountPaise=749900,Status="Active",CreatedUtc=DateTime.UtcNow});await db.SaveChangesAsync();
  var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{["Features:Payments"]="true",["Razorpay:KeyId"]="rzp_test_synthetic",["Razorpay:KeySecret"]="synthetic-only",["Razorpay:WebhookSecret"]="synthetic-webhook"}).Build();
  var provider=new RefundProvider();var service=new RazorpayService(config,new HttpClient(provider){BaseAddress=new Uri("https://example.invalid/")},db);
  string raw=JsonSerializer.Serialize(new{@event="refund.processed",payload=new{refund=new{entity=new{payment_id="pay_refund",amount=1}}}});
  string sign=Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes("synthetic-webhook"),Encoding.UTF8.GetBytes(raw)));
  Assert.True(await service.WebhookAsync(raw,sign,"refund_one",default));Assert.Equal("PartiallyRefunded",(await db.PaymentRecords.SingleAsync()).Status);
  Assert.True(await service.WebhookAsync(raw,sign,"refund_one",default));Assert.Equal(1,await db.ProviderEvents.CountAsync());
  provider.Refunded=749900;Assert.True(await service.WebhookAsync(raw,sign,"refund_two",default));Assert.Equal("Refunded",(await db.PaymentRecords.SingleAsync()).Status);
  provider.Amount=1;Assert.False(await service.WebhookAsync(raw,sign,"refund_bad",default));Assert.Equal(2,await db.ProviderEvents.CountAsync());
  Assert.False(await service.WebhookAsync(raw,new string('0',64),"refund_unsigned",default));Assert.Equal(0,await db.Enrollments.CountAsync());
 }
 private sealed class RefundProvider:HttpMessageHandler
 {
  public int Refunded=10000;public int Amount=749900;
  protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct){Assert.Equal("/payments/pay_refund",request.RequestUri!.AbsolutePath);return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK){Content=new StringContent(JsonSerializer.Serialize(new{id="pay_refund",order_id="order_refund",amount=Amount,currency="INR",amount_refunded=Refunded}))});}
 }
 [Fact] public async Task SignedCapturedEventsAreIdempotentAndRejectTamperedAmounts()
 {
  using var connection=new SqliteConnection("Data Source=:memory:");await connection.OpenAsync();
  await using var db=new ConfidraDbContext(new DbContextOptionsBuilder<ConfidraDbContext>().UseSqlite(connection).Options);await db.Database.EnsureCreatedAsync();
  db.Users.Add(new User{Id=1,FullName="Synthetic",Email="payment@example.invalid",Phone="9000000001",PasswordHash="test-only",CreatedUtc=DateTime.UtcNow});await db.SaveChangesAsync();
  db.PaymentRecords.Add(new PaymentRecord{OrderId="order_synthetic",UserId=1,ProgrammeId="pre-diabetes-90",AmountPaise=749900,CreatedUtc=DateTime.UtcNow});await db.SaveChangesAsync();
  var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{["Features:Payments"]="true",["Razorpay:KeyId"]="rzp_test_synthetic",["Razorpay:KeySecret"]="synthetic-only",["Razorpay:WebhookSecret"]="synthetic-webhook"}).Build();
  var service=new RazorpayService(config,new HttpClient(new NoNetwork()),db);
  string Body(int amount)=>JsonSerializer.Serialize(new{ @event="payment.captured",payload=new{payment=new{entity=new{id="pay_synthetic",order_id="order_synthetic",amount,currency="INR",status="captured"}}}});
  string Sign(string body)=>Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes("synthetic-webhook"),Encoding.UTF8.GetBytes(body)));
  string raw=Body(749900);Assert.True(await service.WebhookAsync(raw,Sign(raw),"evt_1",default));Assert.True(await service.WebhookAsync(raw,Sign(raw),"evt_1",default));
  Assert.Equal(1,await db.ProviderEvents.CountAsync());Assert.Equal(1,await db.AuditEvents.CountAsync());Assert.Equal(0,await db.Enrollments.CountAsync());Assert.Equal("CapturedPendingAssessment",(await db.PaymentRecords.SingleAsync()).Status);
  (await db.PaymentRecords.SingleAsync()).Status="Active";await db.SaveChangesAsync();await service.WebhookAsync(raw,Sign(raw),"evt_again",default);Assert.Equal("Active",(await db.PaymentRecords.SingleAsync()).Status);Assert.Equal(1,await db.AuditEvents.CountAsync());
  string bad=Body(1);Assert.False(await service.WebhookAsync(bad,Sign(bad),"evt_2",default));Assert.False(await service.WebhookAsync(raw,new string('0',64),"evt_3",default));
  Assert.False(await service.VerifyAsync(new VerifyPaymentInput("order_synthetic","pay_synthetic",Sign(raw)),2,default));
 }
 private sealed class NoNetwork:HttpMessageHandler{protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken ct)=>throw new InvalidOperationException("Test must not make provider network calls.");}
}
