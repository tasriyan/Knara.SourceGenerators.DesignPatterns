using CodeGenerator.Patterns.Mediator;
using Demo.Mediator.DotNetCore.VerticalSlices.UserFeatures.Core;
using Microsoft.Extensions.Logging;

namespace Demo.Mediator.DotNetCore.VerticalSlices.UserFeatures;

[Command(Name = "CreateUserCommand", ResponseType = typeof(bool))]
public record CreateUserRequest(int UserId, string Email, string FirstName, string LastName);

[CommandHandler(Name="CreateUserCommandHandler", RequestType = typeof(CreateUserCommand))]
public class CreateUserService(IUserRepository repository, ILogger<CreateUserService> logger)
{
    public async Task<bool> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating user with email {Email}", request.Email);
        var user = new User
        {
            Id = request.UserId,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var success = await repository.CreateAsync(user, cancellationToken);
        if (success)
        {
            logger.LogInformation("User {Email} created successfully", user.Email);
            return true;
        }
        
        logger.LogError("Failed to create user {Email}", user.Email);
        return false;
    }
}
