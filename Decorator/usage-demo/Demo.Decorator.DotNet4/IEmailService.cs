using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Knara.SourceGenerators.DesignPatterns.Decorator;

namespace Demo.Decorator.DotNet4
{
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

	public class EmailService() : IEmailService
	{
		public async Task SendEmailAsync(string to, string subject, string body)
		{
			// Simulate sending email
			await Task.Delay(100);
			Console.WriteLine($"Email sent to {to} with subject '{subject}'");
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
		private readonly int _maxRetries;
		private readonly TimeSpan _baseDelay;

		public RetryEmailServiceDecorator(
			IEmailService inner, 
			int maxRetries = 3,
			TimeSpan? baseDelay = null)
		{
			_inner = inner;
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
						Console.WriteLine("Operation {OperationName} succeeded on attempt {Attempt}", 
							operationName, attempt);
					}
					return;
				}
				catch (Exception ex) when (attempt < _maxRetries)
				{
					var delay = TimeSpan.FromMilliseconds(_baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
					Console.WriteLine($"Operation {operationName} failed on attempt {attempt}. Retrying in {delay.TotalMilliseconds}ms. Error: {ex.Message}", 
						operationName, attempt, delay.TotalMilliseconds, ex.Message);
					await Task.Delay(delay);
				}
			}
		}

		private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName)
		{
			Console.WriteLine($"Starting operation {operationName} with up to {_maxRetries} retries");
			for (int attempt = 1; attempt <= _maxRetries; attempt++)
			{
				try
				{
					var result = await operation();
					if (attempt > 1)
					{
						Console.WriteLine($"Operation {operationName} succeeded on attempt {attempt}");
					}
					Console.WriteLine($"Operation {operationName} completed successfully");
					return result;
				}
				catch (Exception ex) when (attempt < _maxRetries)
				{
					var delay = TimeSpan.FromMilliseconds(_baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
					Console.WriteLine($"Operation {operationName} failed on attempt {attempt}. Retrying in {delay.TotalMilliseconds}ms. Error: {ex.Message}");
					await Task.Delay(delay);
				}
			}
			throw new InvalidOperationException($"Operation {operationName} failed after {_maxRetries} attempts");
		}
	}

	public static class EmailServiceDemo
	{
		public static async Task FluidApiUsageDemo()
		{
			// Create decorated service using the generated factory
			var emailService = EmailDecoratorFactory
				.Create(new EmailService())
				.WithRetry(maxRetries: 5, baseDelay: TimeSpan.FromSeconds(1));
		
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
}
