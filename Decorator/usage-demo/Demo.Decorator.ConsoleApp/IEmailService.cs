using Microsoft.Extensions.Logging;
using SourceGenerators.DesignPatterns.Decorator;

namespace Demo.Decorator.ConsoleApp;

// Example 2: Email Service with one base class

public class EmailTemplate
{
	public string Name { get; set; } = "";
	public string Subject { get; set; } = "";
	public string Body { get; set; } = "";
}

[GenerateDecoratorFactory(BaseName="Email")]
public interface IEmailService
{
	Task SendEmailAsync(string to, string subject, string body);
	
	Task<List<EmailTemplate>> GetEmailTemplatesAsync();
	
	Task<bool> ValidateEmailAddressAsync(string email);
}

public class EmailService(ILogger<EmailService> logger) : IEmailService
{
	public async Task SendEmailAsync(string to, string subject, string body)
	{
		// Simulate sending email
		await Task.Delay(100);
		logger.LogInformation($"Email sent to {to} with subject '{subject}'");
	}

	public async Task<List<EmailTemplate>> GetEmailTemplatesAsync()
	{
		// Simulate fetching email templates
		await Task.Delay(200);
		return new List<EmailTemplate>
		{
			new EmailTemplate { Name = "Welcome", Subject = "Welcome to our service", Body = "Hello, welcome!" },
			new EmailTemplate { Name = "Password Reset", Subject = "Reset your password", Body = "Click here to reset your password." }
		};
	}

	public async Task<bool> ValidateEmailAddressAsync(string email)
	{
		// Simulate email validation
		await Task.Delay(50);
		return email.Contains("@");
	}
}

[Decorator(Type="Retry")]
public class RetryEmailServiceDecorator : IEmailService
{
    private readonly IEmailService _inner;
    private readonly ILogger<RetryEmailServiceDecorator> _logger;
    private readonly int _maxRetries;
    private readonly TimeSpan _baseDelay;

    public RetryEmailServiceDecorator(
        IEmailService inner, 
        ILogger<RetryEmailServiceDecorator> logger,
        int maxRetries = 3,
        TimeSpan? baseDelay = null)
    {
        _inner = inner;
        _logger = logger;
        _maxRetries = maxRetries;
        _baseDelay = baseDelay ?? TimeSpan.FromSeconds(1);
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        await ExecuteWithRetryAsync(
            async () => await _inner.SendEmailAsync(to, subject, body),
            $"SendEmailAsync to {to}");
    }

    public async Task<List<EmailTemplate>> GetEmailTemplatesAsync()
    {
        return await ExecuteWithRetryAsync(
            async () => await _inner.GetEmailTemplatesAsync(),
            "GetEmailTemplatesAsync");
    }

    public async Task<bool> ValidateEmailAddressAsync(string email)
    {
        return await ExecuteWithRetryAsync(
            async () => await _inner.ValidateEmailAddressAsync(email),
            $"ValidateEmailAddressAsync for {email}");
    }

    private async Task ExecuteWithRetryAsync(Func<Task> operation, string operationName)
    {
        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                await operation();
                if (attempt > 1)
                {
                    _logger.LogInformation("Operation {OperationName} succeeded on attempt {Attempt}", 
                        operationName, attempt);
                }
                return;
            }
            catch (Exception ex) when (attempt < _maxRetries)
            {
                var delay = TimeSpan.FromMilliseconds(_baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                _logger.LogWarning("Operation {OperationName} failed on attempt {Attempt}. Retrying in {Delay}ms. Error: {Error}", 
                    operationName, attempt, delay.TotalMilliseconds, ex.Message);
                await Task.Delay(delay);
            }
        }
    }

    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName)
    {
	    _logger.LogInformation("Starting operation {OperationName} with up to {MaxRetries} retries", operationName, _maxRetries);
        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                var result = await operation();
                if (attempt > 1)
                {
                    _logger.LogInformation("Operation {OperationName} succeeded on attempt {Attempt}", 
                        operationName, attempt);
                }
                _logger.LogInformation("Operation {OperationName} completed successfully", operationName);
                return result;
            }
            catch (Exception ex) when (attempt < _maxRetries)
            {
                var delay = TimeSpan.FromMilliseconds(_baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                _logger.LogWarning("Operation {OperationName} failed on attempt {Attempt}. Retrying in {Delay}ms. Error: {Error}", 
                    operationName, attempt, delay.TotalMilliseconds, ex.Message);
                await Task.Delay(delay);
            }
        }
        throw new InvalidOperationException($"Operation {operationName} failed after {_maxRetries} attempts");
    }
}

public static class EmailServiceDemo
{
	public static async Task FluidApiUsageDemo(ILoggerFactory loggerFactory)
	{
		// Setup dependencies
		var logger = loggerFactory.CreateLogger<EmailService>();
		var retryLogger = loggerFactory.CreateLogger<RetryEmailServiceDecorator>();

		// Create decorated service using the generated factory
		var emailService = EmailDecoratorFactory
			.Create(new EmailService(logger))
			.WithRetry(retryLogger, maxRetries: 5, baseDelay: TimeSpan.FromSeconds(1));
		
		// Use the service
		await emailService.SendWelcomeEmailAsync("everybody@fakecompany.com");
	}
	
	private static async Task SendWelcomeEmailAsync(this IEmailService emailService, string to)
	{
		var templates = await emailService.GetEmailTemplatesAsync();
		var welcomeTemplate = templates.FirstOrDefault(t => t.Name == "Welcome");
		if (welcomeTemplate != null)
		{
			await emailService.SendEmailAsync(to, welcomeTemplate.Subject, welcomeTemplate.Body);
		}
		else
		{
			Console.WriteLine("Welcome template not found.");
		}
	}
}
