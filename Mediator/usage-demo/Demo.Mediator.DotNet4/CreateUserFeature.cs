using Knara.SourceGenerators.DesignPatterns.Mediator;
using Demo.Mediator.DotNet4.Core;

namespace Demo.Mediator.DotNet4
{
    [Command(Name = "CreateUserCommand", ResponseType = typeof(bool))]
    public class CreateUserRequest
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    [CommandHandler(Name="CreateUserCommandHandler", RequestType = typeof(CreateUserCommand))]
    public class CreateUserService(IUserRepository repository)
    {
        public async Task<bool> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"Creating user with email {request.Email}");
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
                Console.WriteLine($"User {user.Email} created successfully");
                return true;
            }
        
            Console.WriteLine($"Failed to create user {request.Email}");
            return false;
        }
    }
}
