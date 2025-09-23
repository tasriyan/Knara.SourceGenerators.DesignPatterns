using Knara.SourceGenerators.DesignPatterns.Mediator;
using Demo.Mediator.DotNet4.Core;

namespace Demo.Mediator.DotNet4.Infrastructure
{
    public interface IWelcomeEmailService
    {
        Task ExecuteAsync(UserCreatedEvent userEvent, CancellationToken cancellationToken = default);
    }

    [NotificationHandler(Name="WelcomeEmailHandler", EventType = typeof(UserCreatedEvent))]
    public class WelcomeEmailService(IUserRepository userRepository)
        : IWelcomeEmailService
    {
        public async Task ExecuteAsync(UserCreatedEvent userEvent, CancellationToken cancellationToken = default)
        {
            // Get user details for personalized email
            var user = await userRepository.GetByIdAsync(userEvent.UserId, cancellationToken);
            if (user == null)
            {
                Console.WriteLine($"Cannot send welcome email - user {userEvent.UserId} not found");
                return;
            }

            // Simulate sending email
            Console.WriteLine($"Sending welcome email to {user.Email}");
            await Task.Delay(1000, cancellationToken); // Simulate async work
            Console.WriteLine($"Welcome email sent to {user.Email}");
        }
    }
}