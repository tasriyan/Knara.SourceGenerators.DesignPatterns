# Source Generators Design Patterns - Builder Pattern

A C# source generator that automatically creates Builder pattern implementations for your classes and records. This generator eliminates boilerplate code while providing a fluent, type-safe API for object construction.

## Overview

This source generator analyzes your code at compile time and generates builder classes that follow the Builder design pattern. It supports both classes and records, with extensive customization options through attributes.

## Features

- ✅ **Zero Runtime Dependencies** - Pure compile-time code generation
- ✅ **High Performance** - Optimized source generation with incremental compilation support
- ✅ **Type Safety** - Compile-time validation and type checking
- ✅ **Flexible Configuration** - Extensive attribute-based customization
- ✅ **Multiple Target Frameworks** - Supports .NET Standard 2.0, .NET 8, and .NET 9
- ✅ **Collection Support** - Specialized handling for collections with custom add methods
- ✅ **Validation Support** - Built-in property validation during build
- ✅ **Immutable Objects** - Full support for records and immutable classes

## Performance

Based on comprehensive benchmarks, the generator delivers excellent performance:

| Scenario | Mean Time | Allocated Memory | Notes |
|----------|-----------|------------------|-------|
| Simple Class | 442.5 μs | 128.7 KB | Basic class with 3 properties |
| Complex Class | 781.8 μs | 227.9 KB | Class with validation and collections |
| Large Class | 935.9 μs | 374.75 KB | Class with 10+ properties |
| Multiple Classes | 20.2 ms | 1072.51 KB | 5 classes in one compilation |
| Incremental Generation | 773.4 μs | 231.25 KB | Optimized for fast rebuilds |

*Benchmarks run on .NET 9.0.7, Intel Core i7-10750H*

## Installation

Add the source generator to your project:
```bash
<ItemGroup> <ProjectReference Include="path/to/SourceGenerators.DesignPatterns.Builder.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" /> </ItemGroup>
```

Or via NuGet (when published): 
```bash
dotnet add package SourceGenerators.DesignPatterns.Builder
```

## Quick Start

### 1. Basic Usage
Define a class or record with properties:
```csharp

using SourceGenerators.DesignPatterns.Builder;
[GenerateBuilder] 
public record User
{
	public string FirstName { get; init; } = ""; 
	public string LastName { get; init; } = ""; 
	public int Age { get; init; }
}
```
This generates a `UserBuilder` class that you can use like this:
```csharp
var user = UserBuilder.Create() 
	.WithFirstName("John") 
	.WithLastName("Doe") 
	.WithAge(30) 
	.Build();
```

### 2. Advanced Configuration
```csharp
[GenerateBuilder( ValidateOnBuild = true, BuilderName = "ProjectConfigurationBuilder", GenerateWithMethods = true )]
public record ProjectConfiguration 
{ 
[BuilderProperty(Required = true)] 
public string Name { get; init; } = "";

[BuilderProperty]
public string? Description { get; init; }

[BuilderCollection(AddMethodName = "AddDependency")]
public IReadOnlyList<string> Dependencies { get; init; } = Array.Empty<string>();

[BuilderProperty(ValidatorMethod = nameof(ValidateVersion))]
public Version Version { get; init; } = new(1, 0, 0);

public static bool ValidateVersion(Version version) 
    => version != null && version.Major > 0;
```

Usage:

```csharp
var config = ProjectConfigurationBuilder.Create() 
.WithName("MyProject") 
.WithDescription("A sample project") 
.AddDependency("Microsoft.Extensions.DependencyInjection") 
.AddDependency("Serilog") .WithVersion(new Version(2, 1, 0)) 
.Build(); // Validates that Name is provided and Version is valid
```

## Attributes Reference

### `[GenerateBuilder]`

Main attribute to mark classes/records for builder generation.
```csharp
[GenerateBuilder( 
		ValidateOnBuild = false,	// Enable build-time validation 
		BuilderName = null,			// Custom builder 
		class name GenerateWithMethods = true,   // Generate WithXxx methods 
		GenerateFromMethod = false  // Generate FromXxx method for classes with constructors 
		)]
```

### `[BuilderProperty]`

Configures individual properties.
```csharp
[BuilderProperty(
	Required = false,		// Property must be set before Build() 
	DefaultValue = null,				// Default value as string 
	ValidatorMethod = null,				// Static validation method name 
	CustomSetterName = null,			// Custom method name (instead of WithXxx) 
	AllowNull = true,					// Allow null values 
	IgnoreInBuilder = false				// Exclude from builder 
)]
```

### `[BuilderCollection]`

Special handling for collection properties.
```csharp
[BuilderCollection( 
	AddMethodName = "Add"          // Custom add method name (e.g., "AddItem") 
)]
```

## Supported Scenarios

### Records (Recommended)
```csharp
[GenerateBuilder] 
public record Person(string FirstName, string LastName, int Age);
[GenerateBuilder] 
public record ProjectSettings 
{ 
	public string Name { get; init; } = ""; 
	public List<string> Tags { get; init; } = new(); 
}
```

### Classes with Properties
```csharp
[GenerateBuilder] 
public class User 
{ 
	public string Email { get; set; } = ""; 
	public string FirstName { get; set; } = ""; 
	public bool IsActive { get; set; } 
}
```

### Classes with Constructors
```csharp
[GenerateBuilder(GenerateFromMethod = true)] 
public class DatabaseConfig 
{ 
	public string ConnectionString { get; } 
	public TimeSpan CommandTimeout { get; }

	public DatabaseConfig(string connectionString, TimeSpan? commandTimeout = null)
	{
		ConnectionString = connectionString;
		CommandTimeout = commandTimeout ?? TimeSpan.FromSeconds(30);
	}
}
```

### Collections
```csharp
[GenerateBuilder(ValidateOnBuild = true)] 
public record User 
{ 
	[BuilderProperty(Required = true)] 
	public string Email { get; init; } = "";

	[BuilderProperty(ValidatorMethod = nameof(ValidateAge))]
	public int Age { get; init; }

	public static bool ValidateAge(int age) => age >= 0 && age <= 150;
}
```

### Validation
```csharp
[GenerateBuilder(ValidateOnBuild = true)] 
public record User
{ 
	[BuilderProperty(Required = true)] 
	public string Email { get; init; } = "";
	[BuilderProperty(ValidatorMethod = nameof(ValidateAge))]
	public int Age { get; init; }

	public static bool ValidateAge(int age) => age >= 0 && age <= 150;
}
```


## Generated Code Structure

For a simple class, the generator creates:
```csharp
public sealed class UserBuilder 
{ 
	private string _firstName = ""; 
	private string _lastName = ""; 
	private int _age;
	public static UserBuilder Create() => new();

	public UserBuilder WithFirstName(string firstName)
	{
		_firstName = firstName;
		return this;
	}

	// ... other With methods

	public User Build()
	{
		// Validation (if enabled)
		// Construction logic
		return new User { FirstName = _firstName, ... };
	}

	public User ToBuilder(User source)
	{
		_firstName = source.FirstName;
		// ... copy other properties
		return this;
	}
}
```


## Best Practices

1. **Use Records**: Records work best with this generator due to their immutable nature
2. **Enable Validation**: Use `ValidateOnBuild = true` for critical objects
3. **Mark Required Properties**: Use `[BuilderProperty(Required = true)]` for mandatory fields
4. **Custom Validation**: Implement static validator methods for complex validation logic
5. **Collection Naming**: Use descriptive `AddMethodName` for collections (e.g., "AddDependency" instead of "Add")

## Requirements

- **.NET SDK**: 8.0 or later
- **C# Language Version**: Latest recommended
- **Target Frameworks**: .NET Standard 2.0, .NET 8+, .NET 9+

## Contributing

This project follows modern C# development practices:

- Source generators with incremental compilation
- Comprehensive unit tests
- Performance benchmarks
- Code analysis and nullable reference types

## License

[Add your license information here]

## Changelog

### Version 1.0.0
- Initial release
- Support for classes and records
- Collection handling
- Property validation
- High-performance incremental generation