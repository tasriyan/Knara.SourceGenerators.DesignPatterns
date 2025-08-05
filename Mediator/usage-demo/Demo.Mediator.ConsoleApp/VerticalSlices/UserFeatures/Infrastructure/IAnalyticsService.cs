using CodeGenerator.Patterns.Mediator;
using Demo.Mediator.ConsoleApp.VerticalSlices.UserFeatures.Core;
using Microsoft.Extensions.Logging;

namespace Demo.Mediator.ConsoleApp.VerticalSlices.UserFeatures.Infrastructure;

public interface IAnalyticsService
{
    Task ExecuteAsync(IUserEvent userEvent, CancellationToken cancellationToken = default);
}

[NotificationHandler(Name = "AnalyticsHandler", EventType = typeof(IUserEvent))]
public class AnalyticsService(ILogger<AnalyticsService> logger) : IAnalyticsService
{
    public async Task ExecuteAsync(IUserEvent userEvent, CancellationToken cancellationToken = default)
    {
        if (userEvent is UserCreatedEvent notification)
        {
            logger.LogInformation("Recording analytics for user {UserId} creation", notification.UserId);
        }

        // Simulate analytics tracking
        await Task.Delay(50, cancellationToken);
    }
}