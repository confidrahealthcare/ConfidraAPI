using System.Net.Http.Headers;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ConfidraApi.Common.Models;
using ConfidraApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
namespace ConfidraApi.Business;
public sealed record OrderInput([Required,MaxLength(40)]string ProgrammeId, bool TermsAccepted = false);
public sealed record VerifyPaymentInput([Required,MaxLength(80)]string OrderId, [Required,MaxLength(80)]string PaymentId, [Required,RegularExpression("^[a-fA-F0-9]{64}$")]string Signature);
public sealed class RazorpayService(IConfiguration config, HttpClient client, ConfidraDbContext db)
{
    public static readonly IReadOnlyDictionary<string,int> Prices = new Dictionary<string,int> { ["pre-diabetes-90"] = 749900, ["diabetes-90"] = 1699900 };
    public bool Enabled => string.Equals(config["Features:Payments"], "true", StringComparison.OrdinalIgnoreCase) && (config["Razorpay:KeyId"]?.StartsWith("rzp_test_",StringComparison.Ordinal) ?? false) && !string.IsNullOrWhiteSpace(config["Razorpay:KeySecret"]);
    public string? TermsUrl => Uri.TryCreate(config["Payments:TermsUrl"],UriKind.Absolute,out var uri) && uri.Scheme=="https" && string.IsNullOrEmpty(uri.UserInfo) ? uri.AbsoluteUri : null;
    public bool CheckoutEnabled => Enabled && string.Equals(config["Payments:TermsApproved"],"true",StringComparison.OrdinalIgnoreCase) && TermsUrl is not null;
    private void Configure() { if(!Enabled) throw new InvalidOperationException("Payments are unavailable."); client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Basic",Convert.ToBase64String(Encoding.UTF8.GetBytes(config["Razorpay:KeyId"]+":"+config["Razorpay:KeySecret"]))); }
    public static bool ValidSignature(string payload,string signature,string secret)
    {
        if(string.IsNullOrWhiteSpace(secret)||signature.Length!=64)return false;
        try { return CryptographicOperations.FixedTimeEquals(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret),Encoding.UTF8.GetBytes(payload)),Convert.FromHexString(signature)); } catch(FormatException){return false;}
    }
    public async Task<object> CreateOrderAsync(string programme,int userId,CancellationToken ct)
    {
        Configure();if(!Prices.TryGetValue(programme,out var amount))throw new ArgumentException("Invalid programme.");
        // No diagnosis, contact details or patient identity is sent in provider notes.
        using var response=await client.PostAsJsonAsync("orders",new{amount,currency="INR",receipt=Guid.NewGuid().ToString("N")},ct);
        response.EnsureSuccessStatusCode();using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        string orderId=json.RootElement.GetProperty("id").GetString()!;
        if(json.RootElement.GetProperty("amount").GetInt32()!=amount||json.RootElement.GetProperty("currency").GetString()!="INR")throw new InvalidOperationException("Provider order mismatch.");
        db.PaymentRecords.Add(new PaymentRecord{OrderId=orderId,UserId=userId,ProgrammeId=programme,AmountPaise=amount,CreatedUtc=DateTime.UtcNow});await db.SaveChangesAsync(ct);
        return new{orderId,amount,currency="INR",keyId=config["Razorpay:KeyId"]};
    }
    public async Task<bool> VerifyAsync(VerifyPaymentInput input,int userId,CancellationToken ct)
    {
        Configure();if(input.OrderId.Length>80||input.PaymentId.Length>80||!System.Text.RegularExpressions.Regex.IsMatch(input.PaymentId,@"^pay_[A-Za-z0-9]+$"))return false;
        var order=await db.PaymentRecords.AsNoTracking().SingleOrDefaultAsync(x=>x.OrderId==input.OrderId&&x.UserId==userId,ct);
        if(order is null||!ValidSignature(order.OrderId+"|"+input.PaymentId,input.Signature,config["Razorpay:KeySecret"]!))return false;
        using var response=await client.GetAsync("payments/"+Uri.EscapeDataString(input.PaymentId),ct);response.EnsureSuccessStatusCode();
        using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));var p=json.RootElement;
        if(p.GetProperty("order_id").GetString()!=order.OrderId||p.GetProperty("amount").GetInt32()!=order.AmountPaise||p.GetProperty("currency").GetString()!="INR"||p.GetProperty("status").GetString()!="captured")return false;
        await RecordCaptured(order.OrderId,input.PaymentId,"reconcile:"+input.PaymentId,ct);return true;
    }
    public async Task RecordCaptured(string orderId,string paymentId,string eventId,CancellationToken ct)
    {
        await using var transaction=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable,ct);
        if(await db.ProviderEvents.AnyAsync(x=>x.Id==eventId,ct)){await transaction.CommitAsync(ct);return;}
        var order=await db.PaymentRecords.SingleOrDefaultAsync(x=>x.OrderId==orderId,ct);if(order is null)return;
        if(order.Status=="Refunded"||order.Status=="PartiallyRefunded")return;
        if(order.PaymentId is not null&&order.PaymentId!=paymentId)throw new InvalidOperationException("Payment conflict.");
        var firstCapture = order.PaymentId is null;
        order.PaymentId=paymentId;if(firstCapture)order.Status="CapturedPendingAssessment";
        db.ProviderEvents.Add(new ProviderEvent{Id=eventId,ProcessedUtc=DateTime.UtcNow});
        if(firstCapture)db.AuditEvents.Add(new AuditEvent{ActorId=order.UserId,SubjectId=order.UserId,Action="PaymentCaptured",CreatedUtc=DateTime.UtcNow});
        // Payment is not a clinical approval. Activation requires a separately assessed care pathway.
        await db.SaveChangesAsync(ct);await transaction.CommitAsync(ct);
    }
    public async Task<bool> WebhookAsync(string raw,string signature,string eventId,CancellationToken ct)
    {
        if(!Enabled||eventId.Length is <1 or >150||!ValidSignature(raw,signature,config["Razorpay:WebhookSecret"]??""))return false;
        using var json=JsonDocument.Parse(raw);var root=json.RootElement;var type=root.GetProperty("event").GetString();
        if(type=="refund.processed") {
            var refund=root.GetProperty("payload").GetProperty("refund").GetProperty("entity");
            string? paymentId=refund.GetProperty("payment_id").GetString();
            // Fetch authoritative cumulative refund state; never sum duplicated webhook amounts.
            Configure();if(paymentId is null||!System.Text.RegularExpressions.Regex.IsMatch(paymentId,@"^pay_[A-Za-z0-9]+$"))return false;
            using var response=await client.GetAsync("payments/"+Uri.EscapeDataString(paymentId),ct);response.EnsureSuccessStatusCode();
            using var provider=JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));var payment=provider.RootElement;
            await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable,ct);
            if(await db.ProviderEvents.AnyAsync(x=>x.Id==eventId,ct))return true;
            var record=await db.PaymentRecords.SingleOrDefaultAsync(x=>x.PaymentId==paymentId,ct);
            if(record is null||payment.GetProperty("id").GetString()!=paymentId||payment.GetProperty("order_id").GetString()!=record.OrderId||payment.GetProperty("amount").GetInt32()!=record.AmountPaise||payment.GetProperty("currency").GetString()!="INR")return false;
            int refunded=payment.GetProperty("amount_refunded").GetInt32();if(refunded<=0||refunded>record.AmountPaise)return false;
            record.Status=refunded==record.AmountPaise?"Refunded":"PartiallyRefunded";
            db.ProviderEvents.Add(new ProviderEvent{Id=eventId,ProcessedUtc=DateTime.UtcNow});
            db.AuditEvents.Add(new AuditEvent{ActorId=record.UserId,SubjectId=record.UserId,Action="PaymentRefundReconciled",CreatedUtc=DateTime.UtcNow});
            await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return true;
        }
        if(type!="payment.captured")return true;
        var p=root.GetProperty("payload").GetProperty("payment").GetProperty("entity");
        var order=await db.PaymentRecords.AsNoTracking().SingleOrDefaultAsync(x=>x.OrderId==p.GetProperty("order_id").GetString(),ct);
        if(order is null||p.GetProperty("amount").GetInt32()!=order.AmountPaise||p.GetProperty("currency").GetString()!="INR"||p.GetProperty("status").GetString()!="captured")return false;
        await RecordCaptured(order.OrderId,p.GetProperty("id").GetString()!,eventId,ct);return true;
    }
}
