using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Demo.Mediator.ConsoleApp.VerticalSlices.UserFeatures.Core;

public interface IUserRepository
{
	Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
	Task<bool> CreateAsync(User user, CancellationToken cancellationToken = default);
	Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default);
	Task<int> GetCountAsync(DateTime? createdAfter = null, CancellationToken cancellationToken = default);
	IAsyncEnumerable<User> GetFilteredUsersAsync(string? emailFilter, int bufferSize = 100, [EnumeratorCancellation] CancellationToken cancellationToken = default);
}

public class UserRepository(ILogger<UserRepository> logger) : IUserRepository
{
    // Simulating a database with an in-memory list for demonstration purposes
    private readonly List<User> _users = SeedInitialUsers();

    private static List<User> SeedInitialUsers()
    {
        var baseDate = DateTime.UtcNow.AddMonths(-6); // 6 months ago
        
        return new List<User>
        {
            new User { Id = 1, FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", CreatedAt = baseDate.AddDays(1), UpdatedAt = baseDate.AddDays(1) },
            new User { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane.smith@company.com", CreatedAt = baseDate.AddDays(5), UpdatedAt = baseDate.AddDays(10) },
            new User { Id = 3, FirstName = "Mike", LastName = "Johnson", Email = "mike.johnson@example.com", CreatedAt = baseDate.AddDays(10), UpdatedAt = baseDate.AddDays(15) },
            new User { Id = 4, FirstName = "Sarah", LastName = "Wilson", Email = "sarah.wilson@corp.org", CreatedAt = baseDate.AddDays(15), UpdatedAt = baseDate.AddDays(20) },
            new User { Id = 5, FirstName = "David", LastName = "Brown", Email = "david.brown@example.com", CreatedAt = baseDate.AddDays(20), UpdatedAt = baseDate.AddDays(25) },
            new User { Id = 6, FirstName = "Lisa", LastName = "Davis", Email = "lisa.davis@company.com", CreatedAt = baseDate.AddDays(25), UpdatedAt = baseDate.AddDays(30) },
            new User { Id = 7, FirstName = "Tom", LastName = "Miller", Email = "tom.miller@startup.io", CreatedAt = baseDate.AddDays(30), UpdatedAt = baseDate.AddDays(35) },
            new User { Id = 8, FirstName = "Emma", LastName = "Garcia", Email = "emma.garcia@example.com", CreatedAt = baseDate.AddDays(35), UpdatedAt = baseDate.AddDays(40) },
            new User { Id = 9, FirstName = "Chris", LastName = "Martinez", Email = "chris.martinez@tech.net", CreatedAt = baseDate.AddDays(40), UpdatedAt = baseDate.AddDays(45) },
            new User { Id = 10, FirstName = "Anna", LastName = "Taylor", Email = "anna.taylor@company.com", CreatedAt = baseDate.AddDays(45), UpdatedAt = baseDate.AddDays(50) },
            new User { Id = 11, FirstName = "James", LastName = "Anderson", Email = "james.anderson@example.com", CreatedAt = baseDate.AddDays(50), UpdatedAt = baseDate.AddDays(55) },
            new User { Id = 12, FirstName = "Maria", LastName = "Rodriguez", Email = "maria.rodriguez@global.biz", CreatedAt = baseDate.AddDays(55), UpdatedAt = baseDate.AddDays(60) },
            new User { Id = 13, FirstName = "Alex", LastName = "Thompson", Email = "alex.thompson@startup.io", CreatedAt = baseDate.AddDays(60), UpdatedAt = baseDate.AddDays(65) },
            new User { Id = 14, FirstName = "Jessica", LastName = "White", Email = "jessica.white@corp.org", CreatedAt = baseDate.AddDays(65), UpdatedAt = baseDate.AddDays(70) },
            new User { Id = 15, FirstName = "Robert", LastName = "Lee", Email = "robert.lee@example.com", CreatedAt = baseDate.AddDays(70), UpdatedAt = baseDate.AddDays(75) }
        };
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Fetching user by {UserId}:", id);
        
        // simulate async operation
        await Task.Delay(100, cancellationToken);
        
        return _users.FirstOrDefault(x => x.Id == id);
    }

    public async Task<bool> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        //if user exists, throw exception
        logger.LogDebug("Creating user with {Id}", user.Id);
        if (_users.Any(x => x.Id == user.Id))
        {
            throw new UserNotFoundException(userId: user.Id);
        }
        
        // Simulate async operation
        await Task.Delay(100, cancellationToken);
        
        _users.Add(user);
        logger.LogDebug("Created user {Id} with {Email}:", user.Id, user.Email);
        return true;
    }

    public async Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Updating user with {UserId}", user.Id);
        
        var existingUser = _users.FirstOrDefault(x => x.Id == user.Id);
        if (existingUser == null)
        {
            throw new UserNotFoundException(user.Id);
        }
        // Simulate async operation
        await Task.Delay(100, cancellationToken);
        
        existingUser.Email = user.Email;
        existingUser.FirstName = user.FirstName;
        existingUser.LastName = user.LastName;
        existingUser.UpdatedAt = DateTime.UtcNow;
        
        logger.LogDebug("User {UserId} updated successfully.", user.Id);
        return existingUser; // Return the updated existing user, not the input
    }

    public async Task<int> GetCountAsync(DateTime? createdAfter = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting user count with filter createdAfter {CreateAfterDt}", createdAfter);
        // Simulate async operation
        await Task.Delay(100, cancellationToken);
        
        return _users.Count(x => !createdAfter.HasValue || x.CreatedAt > createdAfter.Value);
    }

    public async IAsyncEnumerable<User> GetFilteredUsersAsync(string? emailFilter, 
        int bufferSize = 50,
        [EnumeratorCancellation]CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting users with filter email {EmailFilter} and buffer size {BufferSize}", emailFilter, bufferSize);
        
        // Simulate async operation
        await Task.Delay(100, cancellationToken);

        var filteredUsers = _users.Where(x => emailFilter == null || x.Email.Contains(emailFilter, StringComparison.OrdinalIgnoreCase)).ToList();
        
        var skip = 0;
        List<User> batch;
        do
        {
            batch = filteredUsers
                .Skip(skip)
                .Take(bufferSize)
                .ToList();

            foreach (var user in batch)
            {
                cancellationToken.ThrowIfCancellationRequested();
                // Add small delay to simulate streaming behavior
                await Task.Delay(10, cancellationToken);
                yield return user;
            }

            skip += batch.Count;
        } while (batch.Count == bufferSize);
    }
}