using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace ConfidraApi.Business;

public sealed class RazorpayOptions
{
    public string KeyId { get; set; } = string.Empty;
    public string KeySecret { get; set; } = string.Empty;
}

public sealed record CreatePaymentOrderRequest(string PlanName, int Amount, int? UserId);
public sealed record PaymentOrderResponse(string OrderId, string KeyId, int Amount, string Currency);

public sealed class RazorpayService(IConfiguration configuration, HttpClient client)
{
    private readonly RazorpayOptions settings = new()
    {
        KeyId = configuration["Razorpay:KeyId"] ?? string.Empty,
        KeySecret = configuration["Razorpay:KeySecret"] ?? string.Empty
    };

    public async Task<PaymentOrderResponse> CreateOrderAsync(
        CreatePaymentOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.KeyId) || string.IsNullOrWhiteSpace(settings.KeySecret) ||
            settings.KeyId.StartsWith("REPLACE_", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Razorpay is not configured. Add Razorpay:KeyId and Razorpay:KeySecret.");
        }

        var allowedAmounts = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["Confidra Core 90"] = 11999,
            ["Confidra Plus 90"] = 17999,
            ["Confidra Continuum 365"] = 39999
        };
        if (!allowedAmounts.TryGetValue(request.PlanName, out var expectedAmount) || request.Amount != expectedAmount)
        {
            throw new ArgumentException("The selected programme or amount is invalid.");
        }

        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{settings.KeyId}:{settings.KeySecret}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        using var response = await client.PostAsJsonAsync("orders", new
        {
            amount = request.Amount * 100,
            currency = "INR",
            receipt = $"confidra_{Guid.NewGuid():N}",
            notes = new { plan = request.PlanName, userId = request.UserId }
        }, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Razorpay order creation failed: {content}");
        }

        using var order = JsonDocument.Parse(content);
        return new PaymentOrderResponse(
            order.RootElement.GetProperty("id").GetString()!,
            settings.KeyId,
            order.RootElement.GetProperty("amount").GetInt32(),
            order.RootElement.GetProperty("currency").GetString()!);
    }
}
