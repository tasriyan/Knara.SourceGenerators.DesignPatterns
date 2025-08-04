namespace Demo.Mediator.ConsoleApp.Core;

public interface IEmailService
{
    Task SendWelcomeEmailAsync(int userId, CancellationToken cancellationToken = default);
}