namespace Demo.Mediator.ConsoleApp.VerticalSlices.UserFeatures.Core;

public class User
{
	public int Id { get; set; }
	public string Email { get; set; } = "";
	public string FirstName { get; set; } = "";
	public string LastName { get; set; } = "";
	public DateTime CreatedAt { get; set; }
	public DateTime? UpdatedAt { get; set; }

	public static User UserNotFound(int userId) => new User
		{
			Id = userId,
			Email = "Not Found",
			FirstName = "Not Found",
			LastName = "Not Found",
			CreatedAt = DateTime.MinValue,
			UpdatedAt = null
		};
}

public interface IUserEvent
{
	int UserId { get; }
	DateTime Timestamp { get; }
}

public class UserCreatedEvent : IUserEvent
{
	public int UserId { get; set; }
	public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class UserNotFoundException(int userId) : Exception($"User with ID {userId} not found");

public class UserAlreadyExists(int userId) : Exception($"User with ID {userId} already exists in database.");