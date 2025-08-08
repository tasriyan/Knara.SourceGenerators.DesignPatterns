using CodeGenerator.Patterns.Mediator;
using Demo.Mediator.DotNet4.Core;

namespace Demo.Mediator.DotNet4.Infrastructure
{
    public interface IUserActivityLogger
    {
        Task ExecuteAsync(IUserEvent userEvent, CancellationToken cancellationToken = default);
    }

    [NotificationHandler(Name="LogUserCreationHandler", EventType = typeof(IUserEvent))]
    public class UserActivityLogger: IUserActivityLogger
    {
        public async Task ExecuteAsync(IUserEvent userEvent, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"Activity recorded for {userEvent.UserId} at {userEvent.Timestamp}");
        
            // Simulate some async work
            await Task.Delay(100, cancellationToken);
        }
    }
}