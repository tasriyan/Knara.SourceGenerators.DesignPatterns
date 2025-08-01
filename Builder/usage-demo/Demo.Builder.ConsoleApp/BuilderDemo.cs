using System.Text.Json;
using Demo.Builder.ConsoleApp.SampleModels;

namespace Demo.Builder.ConsoleApp;

internal static class BuilderDemo
{
    public static void RunDemo()
    {
        Console.WriteLine("=== Builder Pattern Generator BuilderGenerator.Demo ===\n");

        // BuilderGenerator.Demo 1: User Builder
        DemoUserBuilder();
        
        // BuilderGenerator.Demo 2: Project Configuration Builder
        DemoProjectConfigurationBuilder();
        
        // BuilderGenerator.Demo 3: API Client Config Builder
        DemoApiClientConfigBuilder();
        
        // BuilderGenerator.Demo 4: Database Config Builder
        DemoDatabaseConfigBuilder();
        
        // BuilderGenerator.Demo 5: Error Handling
        DemoErrorHandling();
        
        // BuilderGenerator.Demo 6: Builder vs Direct Comparison
        DemoBuilderVsDirectComparison();

        Console.WriteLine("\n=== BuilderGenerator.Demo Complete ===");
        Console.ReadKey();
    }

    static void DemoUserBuilder()
    {
        Console.WriteLine("1. USER BUILDER DEMO");
        Console.WriteLine("===================");

        // Create user with builder
        var user = UserBuilder.Create()
            .WithEmail("john.doe@example.com")
            .WithFirstName("John")
            .WithLastName("Doe")
            .WithAge(30)
            .WithIsActive(true)
            .Build();

        Console.WriteLine($"Created User:");
        Console.WriteLine($"  Email: {user.Email}");
        Console.WriteLine($"  Name: {user.FirstName} {user.LastName}"); 
        Console.WriteLine($"  Age: {user.Age}");
        Console.WriteLine($"  Active: {user.IsActive}");
        Console.WriteLine($"  Created: {user.CreatedAt:yyyy-MM-dd HH:mm:ss}");

        // Test ToBuilder extension
        var modifiedUser = user.ToBuilder()
            .WithAge(31)
            .WithLastName("Smith")
            .Build();

        Console.WriteLine($"\nModified User (using ToBuilder):");
        Console.WriteLine($"  Email: {modifiedUser.Email}");
        Console.WriteLine($"  Name: {modifiedUser.FirstName} {modifiedUser.LastName}");
        Console.WriteLine($"  Age: {modifiedUser.Age}");

        Console.WriteLine();
    }

    static void DemoProjectConfigurationBuilder()
    {
        Console.WriteLine("2. PROJECT CONFIGURATION BUILDER DEMO");
        Console.WriteLine("=====================================");

        var config = ProjectConfigurationBuilder.Create()
            .WithName("MyAwesomeProject")
            .WithDescription("A demonstration project for the builder pattern")
            .AddDependency("Microsoft.Extensions.DependencyInjection")
            .AddDependency("Serilog")
            .AddDependency("AutoMapper")
            .AddTag("backend")
            .AddTag("api")
            .AddTag("microservice")
            .WithVersion(new Version(2, 1, 0))
            .Build();

        Console.WriteLine($"Project Configuration:");
        Console.WriteLine($"  Name: {config.Name}");
        Console.WriteLine($"  Description: {config.Description}");
        Console.WriteLine($"  Version: {config.Version}");
        Console.WriteLine($"  Dependencies ({config.Dependencies.Count}):");
        foreach (var dep in config.Dependencies)
        {
            Console.WriteLine($"    - {dep}");
        }
        Console.WriteLine($"  Tags ({config.Tags.Count}):");
        foreach (var tag in config.Tags)
        {
            Console.WriteLine($"    - {tag}");
        }

        Console.WriteLine();
    }

    static void DemoApiClientConfigBuilder()
    {
        Console.WriteLine("3. API CLIENT CONFIG BUILDER DEMO");
        Console.WriteLine("=================================");

        var config = ApiConfigBuilder.Create()
            .WithBaseUrl("https://api.example.com/v1")
            .WithTimeout(TimeSpan.FromSeconds(45))
            .WithAuthType(AuthenticationType.ApiKey)
            .WithApiKey("super-secret-key-123")
            .WithRetryAttempts(3)
            .AddDefaultHeader("X-API-Version: v1")
            .AddDefaultHeader("Accept: application/json")
            .Build();

        Console.WriteLine($"API Client Configuration:");
        Console.WriteLine($"  Base URL: {config.BaseUrl}");
        Console.WriteLine($"  Timeout: {config.Timeout.TotalSeconds} seconds");
        Console.WriteLine($"  Auth Type: {config.AuthType}");
        Console.WriteLine($"  API Key: {config.ApiKey}");
        Console.WriteLine($"  Retry Attempts: {config.RetryAttempts}");
        Console.WriteLine($"  Default Headers ({config.DefaultHeaders.Count}):");
        foreach (var header in config.DefaultHeaders)
        {
            Console.WriteLine($"    - {header}");
        }

        Console.WriteLine();
    }

    static void DemoDatabaseConfigBuilder()
    {
        Console.WriteLine("4. DATABASE CONFIG BUILDER DEMO");
        Console.WriteLine("===============================");

        var config = DatabaseConfigBuilder.Create()
            .WithConnectionString("Server=localhost;Database=MyApp;Trusted_Connection=true")
            .WithCommandTimeout(TimeSpan.FromMinutes(2))
            .WithPoolSize(150) // Using custom setter name
            .WithEnableRetry(true)
            .AddTag("production")
            .AddTag("primary")
            .AddTag("read-write")
            .Build();

        Console.WriteLine($"Database Configuration:");
        Console.WriteLine($"  Connection String: {config.ConnectionString}");
        Console.WriteLine($"  Command Timeout: {config.CommandTimeout.TotalMinutes} minutes");
        Console.WriteLine($"  Max Pool Size: {config.MaxPoolSize}");
        Console.WriteLine($"  Enable Retry: {config.EnableRetry}");
        Console.WriteLine($"  Tags ({config.Tags.Count}):");
        foreach (var tag in config.Tags)
        {
            Console.WriteLine($"    - {tag}");
        }

        Console.WriteLine();
    }

    static void DemoErrorHandling()
    {
        Console.WriteLine("5. ERROR HANDLING DEMO");
        Console.WriteLine("=====================");

        try
        {
            // Try to build user without required fields
            var builder = UserBuilder.Create()
                .WithFirstName("John");

            Console.WriteLine($"Can build: {builder.CanBuild()}");
            
            var missing = builder.GetMissingRequiredProperties();
            Console.WriteLine($"Missing required properties: {string.Join(", ", missing)}");

            Console.WriteLine("Attempting to build incomplete user...");
            var user = builder.Build(); // This should throw
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"✓ Expected error caught: {ex.Message}");
        }

        try
        {
            // Try invalid age validation
            Console.WriteLine("Attempting to set invalid age...");
            UserBuilder.Create()
                .WithEmail("test@example.com")
                .WithFirstName("Test")
                .WithAge(-10); // This should throw
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"✓ Expected validation error caught: {ex.Message}");
        }

        try
        {
            // Try invalid timeout validation
            Console.WriteLine("Attempting to set invalid database timeout...");
            DatabaseConfigBuilder.Create()
                .WithConnectionString("test")
                .WithCommandTimeout(TimeSpan.FromMinutes(15)); // This should throw (>10 minutes)
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"✓ Expected validation error caught: {ex.Message}");
        }

        Console.WriteLine();
    }

    static void DemoBuilderVsDirectComparison()
    {
        Console.WriteLine("6. BUILDER VS DIRECT INSTANTIATION COMPARISON");
        Console.WriteLine("=============================================");

        // Using Builder
        var startTime = DateTime.Now;
        var userFromBuilder = UserBuilder.Create()
            .WithEmail("comparison@example.com")
            .WithFirstName("Builder")
            .WithLastName("User")
            .WithAge(25)
            .WithIsActive(true)
            .Build();
        var builderTime = DateTime.Now - startTime;

        // Direct instantiation
        startTime = DateTime.Now;
        var userDirect = new User
        {
            Email = "comparison@example.com",
            FirstName = "Direct",
            LastName = "User",
            Age = 25,
            IsActive = true
        };
        var directTime = DateTime.Now - startTime;

        Console.WriteLine("Builder Pattern Result:");
        Console.WriteLine($"  Email: {userFromBuilder.Email}");
        Console.WriteLine($"  Name: {userFromBuilder.FirstName} {userFromBuilder.LastName}");
        Console.WriteLine($"  Age: {userFromBuilder.Age}");
        Console.WriteLine($"  Active: {userFromBuilder.IsActive}");
        Console.WriteLine($"  Creation Time: {builderTime.TotalMilliseconds:F2}ms");

        Console.WriteLine("\nDirect Instantiation Result:");
        Console.WriteLine($"  Email: {userDirect.Email}");
        Console.WriteLine($"  Name: {userDirect.FirstName} {userDirect.LastName}");
        Console.WriteLine($"  Age: {userDirect.Age}");
        Console.WriteLine($"  Active: {userDirect.IsActive}");
        Console.WriteLine($"  Creation Time: {directTime.TotalMilliseconds:F2}ms");

        Console.WriteLine("\nBuilder Pattern Benefits:");
        Console.WriteLine("  ✓ Fluent API for better readability");
        Console.WriteLine("  ✓ Required field validation");
        Console.WriteLine("  ✓ Property validation during construction");
        Console.WriteLine("  ✓ Immutable object creation");
        Console.WriteLine("  ✓ Easy object modification via ToBuilder()");
        Console.WriteLine("  ✓ Collection handling with helper methods");

        // Demonstrate JSON serialization comparison
        var builderJson = JsonSerializer.Serialize(userFromBuilder, new JsonSerializerOptions { WriteIndented = true });
        var directJson = JsonSerializer.Serialize(userDirect, new JsonSerializerOptions { WriteIndented = true });

        Console.WriteLine($"\nSerialized objects are functionally equivalent:");
        Console.WriteLine($"Builder JSON length: {builderJson.Length} characters");
        Console.WriteLine($"Direct JSON length: {directJson.Length} characters");

        Console.WriteLine();
    }
}