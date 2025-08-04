using CodeGenerator.Patterns.Mediator;
using Demo.Mediator.ConsoleApp.Core;
using Microsoft.Extensions.Logging;

namespace Demo.Mediator.ConsoleApp.VerticalSlices;

[Command(Name = "CreateUserCommand", ResponseType = typeof(bool))]
public record CreateUserRequest(int UserId, string Email, string FirstName, string LastName);

[CommandHandler(Name="CreateUserCommandHandler", RequestType = typeof(CreateUserCommand))]//, PublisherType = typeof(CreateUserEventPublisher))]
public class CreateUserService(IUserRepository repository, ILogger<CreateUserService> logger)
{
	// private readonly IEventPublisher _eventPublisher;
	public async Task<bool> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
	{
		logger.LogInformation("Creating user with email {Email}", request.Email);
		var user = new User
		{
			Id = request.UserId,
			Email = request.Email,
			FirstName = request.FirstName,
			LastName = request.LastName,
			CreatedAt = DateTime.UtcNow
		};

		if (await repository.CreateAsync(user, cancellationToken))
		{
			logger.LogInformation("User {Email} created successfully", user.Email);
			return true;
		}
		
		logger.LogError("Failed to create user {Email}", user.Email);
		return false;
		
		// Publish domain event (one-to-many handlers)
		// await _eventPublisher.Publish(new UserCreatedEvent { UserId = user.Id });
	}
}

// TODO: NOTIFICATION PATTERN - Define Models and Implementation regular way and use attributes to generate the mediator code
// [Notification]
// public class UserCreatedEvent
// {
//     public int UserId { get; set; }
//     public DateTime Timestamp { get; set; } = DateTime.UtcNow;
// }
//
// [NotificationHandler(Name="LogUserCreationHandler", EventType = typeof(UserCreatedEvent))]
// public class UserCreationLogger(ILogger<LogUserCreationHandler> logger)
// {
//     public async Task ProcessAsync(UserCreatedEvent notification, CancellationToken cancellationToken = default)
//     {
//         logger.LogInformation("User {UserId} created at {Timestamp}",
//             notification.UserId, notification.Timestamp);
//     }
// }
//
// [NotificationHandler(Name="SendWelcomeEmailHandler", EventType = typeof(UserCreatedEvent))]
// public class WelcomeEmailSender(IEmailService emailService) 
// {
//     public async Task ProcessAsync(UserCreatedEvent notification, CancellationToken cancellationToken = default)
//     {
//         await emailService.SendWelcomeEmailAsync(notification.UserId, cancellationToken);
//     }
// }
