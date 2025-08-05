using CodeGenerator.Patterns.Mediator;
using Demo.Mediator.ConsoleApp.VerticalSlices.UserFeatures.Core;
using Microsoft.Extensions.Logging;

namespace Demo.Mediator.ConsoleApp.VerticalSlices.UserFeatures.Infrastructure;

public interface IUserActivityLogger
{
    Task ExecuteAsync(IUserEvent userEvent, CancellationToken cancellationToken = default);
}

[NotificationHandler(Name="LogUserCreationHandler", EventType = typeof(IUserEvent))]
public class UserActivityLogger(ILogger<UserActivityLogger> logger): IUserActivityLogger
{
    public async Task ExecuteAsync(IUserEvent userEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Activity recorded for {UserId} at {Timestamp}",
            userEvent.UserId, userEvent.Timestamp);
        
        // Simulate some async work
        await Task.Delay(100, cancellationToken);
    }
}