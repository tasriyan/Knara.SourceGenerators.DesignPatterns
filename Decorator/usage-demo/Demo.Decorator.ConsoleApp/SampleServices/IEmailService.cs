namespace Demo.Decorator.ConsoleApp.SampleServices;

public class EmailTemplate
{
	public string Name { get; set; } = "";
	public string Subject { get; set; } = "";
	public string Body { get; set; } = "";
}

// Example 2: Email Service with retry and logging
[GenerateDecorators(BaseName = "Email")]
public interface IEmailService
{
	[Decorator(Type = DecoratorType.Retry, RetryAttempts = 5)]
	[Decorator(Type = DecoratorType.Logging)]
	[Decorator(Type = DecoratorType.Performance)]
	Task SendEmailAsync(string to, string subject, string body);

	[Decorator(Type = DecoratorType.Caching, CacheExpirationMinutes = 60)]
	[Decorator(Type = DecoratorType.Logging)]
	Task<List<EmailTemplate>> GetEmailTemplatesAsync();

	[Decorator(Type = DecoratorType.Validation)]
	[Decorator(Type = DecoratorType.Logging)]
	Task<bool> ValidateEmailAddressAsync(string email);
}
