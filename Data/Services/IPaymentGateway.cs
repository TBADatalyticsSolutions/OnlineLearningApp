namespace OnlineLearningApp.Data.Services;

public interface IPaymentGateway
{
    Task<string> InitializeAsync(string email, decimal amountNaira, string reference, string callbackUrl, CancellationToken cancellationToken = default);
    Task<PaymentVerificationResult> VerifyAsync(string reference, CancellationToken cancellationToken = default);
}

public sealed record PaymentVerificationResult(bool IsSuccessful, decimal AmountNaira, string Reference, string Status);
