using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace OnlineLearningApp.Data.Services.Implementation;

public sealed class PaystackPaymentGateway : IPaymentGateway
{
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;
    public PaystackPaymentGateway(HttpClient http, IConfiguration configuration) { _http = http; _configuration = configuration; }

    public async Task<string> InitializeAsync(string email, decimal amountNaira, string reference, string callbackUrl, CancellationToken cancellationToken = default)
    {
        var secret = _configuration["Paystack:SecretKey"];
        if (string.IsNullOrWhiteSpace(secret)) throw new InvalidOperationException("Paystack:SecretKey is not configured.");
        using var request = new HttpRequestMessage(HttpMethod.Post, "transaction/initialize");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret);
        request.Content = JsonContent.Create(new { email, amount = ((long)Math.Round(amountNaira * 100m)).ToString(), currency = "NGN", reference, callback_url = callbackUrl });
        using var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<PaystackInitializeResponse>(cancellationToken: cancellationToken);
        if (payload is null || !payload.Status || string.IsNullOrWhiteSpace(payload.Data?.AuthorizationUrl)) throw new InvalidOperationException("Paystack did not return a checkout URL.");
        return payload.Data.AuthorizationUrl;
    }

    public async Task<PaymentVerificationResult> VerifyAsync(string reference, CancellationToken cancellationToken = default)
    {
        var secret = _configuration["Paystack:SecretKey"];
        if (string.IsNullOrWhiteSpace(secret)) throw new InvalidOperationException("Paystack:SecretKey is not configured.");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"transaction/verify/{Uri.EscapeDataString(reference)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret);
        using var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<PaystackVerifyResponse>(cancellationToken: cancellationToken);
        var data = payload?.Data;
        var amount = data?.Amount is null ? 0m : data.Amount.Value / 100m;
        return new PaymentVerificationResult(payload?.Status == true && string.Equals(data?.Status, "success", StringComparison.OrdinalIgnoreCase), amount, data?.Reference ?? reference, data?.Status ?? "unknown");
    }

    private sealed class PaystackInitializeResponse { public bool Status { get; set; } public PaystackInitializeData? Data { get; set; } }
    private sealed class PaystackInitializeData { [JsonPropertyName("authorization_url")] public string? AuthorizationUrl { get; set; } }
    private sealed class PaystackVerifyResponse { public bool Status { get; set; } public PaystackVerifyData? Data { get; set; } }
    private sealed class PaystackVerifyData { public string? Status { get; set; } public long? Amount { get; set; } public string? Reference { get; set; } }
}
