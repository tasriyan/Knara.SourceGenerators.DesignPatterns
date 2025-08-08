using CodeGenerator.Patterns.Mediator;
using Demo.Mediator.DotNetCore.VerticalSlices.UserFeatures.Core;
using Microsoft.Extensions.Logging;

namespace Demo.Mediator.DotNetCore.VerticalSlices.UserFeatures.Infrastructure;
public interface IWelcomeEmailService
{
    Task ExecuteAsync(UserCreatedEvent userEvent, CancellationToken cancellationToken = default);
}

[NotificationHandler(Name="WelcomeEmailHandler", EventType = typeof(UserCreatedEvent))]
public class WelcomeEmailService(ILogger<WelcomeEmailService> logger, IUserRepository userRepository)
    : IWelcomeEmailService
{
    public async Task ExecuteAsync(UserCreatedEvent userEvent, CancellationToken cancellationToken = default)
    {
        // Get user details for personalized email
        var user = await userRepository.GetByIdAsync(userEvent.UserId, cancellationToken);
        if (user == null)
        {
            logger.LogWarning("Cannot send welcome email - user {UserId} not found", userEvent.UserId);
            return;
        }

        // Simulate sending email
        logger.LogInformation("Sending welcome email to {Email}", user.Email);
        await Task.Delay(1000, cancellationToken); // Simulate async work
        logger.LogInformation("Welcome email sent to {Email}", user.Email);
    }
}