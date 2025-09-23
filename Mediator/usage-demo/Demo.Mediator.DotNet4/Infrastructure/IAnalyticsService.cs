using Knara.SourceGenerators.DesignPatterns.Mediator;
using Demo.Mediator.DotNet4.Core;

namespace Demo.Mediator.DotNet4.Infrastructure
{
    public interface IAnalyticsService
    {
        Task ExecuteAsync(IUserEvent userEvent, CancellationToken cancellationToken = default);
    }

    [NotificationHandler(Name = "AnalyticsHandler", EventType = typeof(IUserEvent))]
    public class AnalyticsService() : IAnalyticsService
    {
        public async Task ExecuteAsync(IUserEvent userEvent, CancellationToken cancellationToken = default)
        {
            if (userEvent is UserCreatedEvent notification)
            {
                Console.WriteLine($"Recording analytics for user {notification.UserId} creation");
            }

            // Simulate analytics tracking
            await Task.Delay(50, cancellationToken);
        }
    }
}