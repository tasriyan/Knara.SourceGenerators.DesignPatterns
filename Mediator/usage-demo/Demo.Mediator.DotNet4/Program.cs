using CodeGenerator.Patterns.Mediator;

namespace Demo.Mediator.DotNet4
{
    internal class Program
    {
        public static void Main(string[] args)
        {
	        var mediator = new GeneratedMediator();
            var legacyDemo = new MediatorWithLegacyCodeConversionDemo(mediator);
            legacyDemo.DemonstrateUsage().GetAwaiter().GetResult();
        }
    }
    
    public class MediatorWithLegacyCodeConversionDemo(IMediator mediator)
    {
    	public async Task DemonstrateUsage()
    	{
    		Console.WriteLine("\n=================================");
    		Console.WriteLine("Source Generated Mediator Demo with Legacy Code Conversion");
    		Console.WriteLine("=================================");
    
    		var random = new Random();
    		var userId = random.Next(1000, 9999);
    		
    		// 1. Create a new user
    		await mediator.Send(
    			new LegacyUserCreateRequest
    			{
    				Model = new NewUserModel
    				{
    					UserId = userId,
    					Email = "arabella@example.com",
    					FirstName = "Arabella",
    					LastName = "Smith"
    				}
    			});
    		
    		// 3. Fetch user by id
    		var user = await mediator.Send(new LegacyUserGetRequest {
    			UserId = userId
    		});
    
    		// 4. UpdateUser
    		var updatedUser = await mediator.Send(new LegacyUserUpdateUserRequest
    		{
    			UserId = userId,
    			Email = "arabella.smith@example.com",
    			FirstName = "Bella"
    		});
    		if (updatedUser != null)
    		{
    			Console.WriteLine($"User updated successfully: {updatedUser.FirstName} {updatedUser.LastName} ({updatedUser.Email})");
    		}
    		else
    		{
    			Console.WriteLine($"Failed to update user with ID: {userId}");
    		}
    	}
    }
}