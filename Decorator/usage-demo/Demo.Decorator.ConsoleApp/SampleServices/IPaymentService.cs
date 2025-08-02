using Demo.Generator;

namespace Demo.Decorator.ConsoleApp.SampleServices;

public class PaymentRequest
{
	public string UserId { get; set; } = "";
	public decimal Amount { get; set; }
	public string Currency { get; set; } = "";
	public string PaymentMethodId { get; set; } = "";
}

public class PaymentResult
{
	public bool Success { get; set; }
	public string TransactionId { get; set; } = "";
	public string ErrorMessage { get; set; } = "";
}

public class PaymentMethod
{
	public string Id { get; set; } = "";
	public string Type { get; set; } = "";
	public string LastFourDigits { get; set; } = "";
}

// Example 4: Payment Service with comprehensive error handling
[GenerateDecorators(BaseName = "Payment")]
public interface IPaymentService
{
	[Decorator(Type = DecoratorType.Validation)]
	[Decorator(Type = DecoratorType.Retry, RetryAttempts = 3)]
	[Decorator(Type = DecoratorType.Logging)]
	[Decorator(Type = DecoratorType.Performance)]
	Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);

	[Decorator(Type = DecoratorType.Caching, CacheExpirationMinutes = 120)]
	[Decorator(Type = DecoratorType.Logging)]
	Task<List<PaymentMethod>> GetPaymentMethodsAsync(string userId);

	[Decorator(Type = DecoratorType.Validation)]
	[Decorator(Type = DecoratorType.Logging)]
	Task<bool> RefundPaymentAsync(string paymentId, decimal amount);
}
