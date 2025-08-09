# Builder Pattern Generator

A C# source generator that creates fluent builder classes for complex object construction in .NET Framework applications. Automatically generates type-safe builders with validation, required field checking, and collection handling.

## Why This Generator Exists

**Legacy Framework Reality**: .NET Framework 4.x lacks modern language features like `init` properties, nullable reference types, and advanced object initializers. Creating immutable, validated objects requires verbose constructors or error-prone manual builder implementations.

**Team Skill Constraints**: Implementing correct builder patterns manually requires understanding of fluent interfaces, validation chains, and immutability patterns that inexperienced developers often get wrong.

**Solution**: Generate proven, consistent builder implementations that provide modern object construction patterns for legacy frameworks.

## Quick Start
Add the source generator to your project:
```bash
<ItemGroup> <ProjectReference Include="path/to/CodeGenerator.DesignPatterns.Builder.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" /> </ItemGroup>
```

Or via NuGet (when published):
```bash
dotnet add package CodeGenerator.DesignPatterns.Builder
```
If you are using the generator in .net 4.+ projects, refer to [this guide](../dotnet-legacy-guide.md) for additional steps.

```csharp
using CodeGenerator.Patterns.Builder;

[GenerateBuilder]
public class User
{
    [BuilderProperty(Required = true)]
    public string FirstName { get; }
    
    [BuilderProperty]
    public string LastName { get; }
    
    [BuilderProperty(ValidatorMethod = nameof(ValidateAge))]
    public int Age { get; }
    
    public User(string firstName, string lastName, int age)
    {
        FirstName = firstName;
        LastName = lastName; 
        Age = age;
    }
    
    public static bool ValidateAge(int age) => age >= 0 && age <= 150;
}

// Usage
var user = UserBuilder.Create()
    .WithFirstName("John")
    .WithLastName("Doe") 
    .WithAge(30)
    .Build();
```

## Builder Configuration

### Class-Level Attributes

```csharp
[GenerateBuilder(
    BuilderName = "CustomBuilder",           // Override default name
    ValidateOnBuild = true,                  // Check required fields
    GenerateWithMethods = true,              // Generate WithXxx methods  
    GenerateFromMethod = true,               // Generate From() method
    Accessibility = BuilderAccessibility.Internal  // Control visibility
)]
```

### Property-Level Attributes

```csharp
[BuilderProperty(
    Required = true,                         // Must be set before Build()
    ValidatorMethod = "ValidateEmail",       // Custom validation method
    DefaultValue = "\"Unknown\"",            // Default value
    CustomSetterName = "WithEmailAddress",  // Override method name
    AllowNull = false,                       // Null checking
    IgnoreInBuilder = true                   // Exclude from builder
)]
```

### Collection Attributes

```csharp
[BuilderCollection(
    AddMethodName = "AddTag",                // Single item method name
    AddRangeMethodName = "AddTags",          // Range method name  
    GenerateClearMethod = true,              // Generate Clear method
    GenerateCountProperty = true             // Generate Count property
)]
```

## Use Cases by Complexity

### 1. Simple Configuration Objects
**When**: Objects with optional parameters and defaults
```csharp
[GenerateBuilder]
public class DatabaseConfig
{
    [BuilderProperty(Required = true)]
    public string ConnectionString { get; }
    
    [BuilderProperty]
    public TimeSpan Timeout { get; } = TimeSpan.FromSeconds(30);
    
    [BuilderProperty] 
    public int MaxRetries { get; } = 3;
}
```

### 2. Domain Entities with Validation
**When**: Business objects needing validation before construction
```csharp
[GenerateBuilder(ValidateOnBuild = true)]
public class Customer
{
    [BuilderProperty(Required = true, ValidatorMethod = nameof(ValidateEmail))]
    public string Email { get; }
    
    [BuilderProperty(Required = true)]
    public string Name { get; }
    
    [BuilderProperty(ValidatorMethod = nameof(ValidateAge))]
    public int Age { get; }
    
    public static bool ValidateEmail(string email) => email.Contains("@");
    public static bool ValidateAge(int age) => age >= 18;
}
```

### 3. Complex Objects with Collections
**When**: Objects with multiple collections and complex structure
```csharp
[GenerateBuilder]
public class ProjectConfiguration
{
    [BuilderProperty(Required = true)]
    public string Name { get; }
    
    [BuilderCollection(AddMethodName = "AddDependency")]
    public IReadOnlyList<string> Dependencies { get; }
    
    [BuilderCollection]
    public List<string> Tags { get; }
    
    [BuilderProperty]
    public Dictionary<string, string> Metadata { get; }
}
```

### 4. API Configuration Objects
**When**: Service configurations with many optional parameters
```csharp
[GenerateBuilder]
public class ApiClientConfig
{
    [BuilderProperty(Required = true)]
    public string BaseUrl { get; }
    
    [BuilderProperty]
    public TimeSpan Timeout { get; } = TimeSpan.FromSeconds(30);
    
    [BuilderProperty]
    public AuthenticationType AuthType { get; } = AuthenticationType.None;
    
    [BuilderCollection]
    public IReadOnlyList<string> DefaultHeaders { get; }
}
```

## Generated Features

### Core Builder Methods
- `Create()` - Static factory method
- `WithXxx()` - Fluent setters for each property
- `Build()` - Creates the final object
- `From(existing)` - Initialize from existing object
- `ToBuilder()` - Extension method for existing objects

### Collection Support
- `AddXxx()` - Add single items
- `AddXxxs()` - Add multiple items
- `ClearXxx()` - Clear collections
- `XxxCount` - Get collection counts

### Validation Features
- Required field checking
- Custom validation methods
- Null value validation
- Build-time validation with clear error messages

## Legacy Framework Benefits

### ✅ Solves Legacy Problems
- **Immutable objects** without `init` properties
- **Validation** without nullable reference types
- **Fluent APIs** without manual implementation
- **Required fields** without compiler support
- **Collection building** with type safety

### ✅ Team Safety Features
- **Generated validation** prevents runtime errors
- **Consistent patterns** across codebase
- **Clear error messages** for missing required fields
- **Type-safe builders** eliminate casting errors

## Performance Considerations

### Memory Usage
- **Additional allocations**: Builder instances and intermediate collections
- **Garbage collection**: More objects to collect during build process

### Build Performance
- **Compile-time generation**: No runtime reflection overhead
- **Direct method calls**: Faster than dynamic construction
- **Validation overhead**: Custom validators run at build time

## Best Practices

### ✅ Good Uses
- **Configuration objects** with many optional parameters
- **Domain entities** requiring validation
- **API builders** for complex integrations
- **Data transfer objects** needing immutability
- **Test data builders** for unit tests

### ❌ Avoid For
- **Simple DTOs** with 2-3 properties
- **Performance-critical paths** with high allocation rates
- **Value objects** better suited for constructors
- **Objects changing frequently** (maintenance overhead)

### Design Guidelines
- **Keep builders focused**: One builder per aggregate root
- **Use validation sparingly**: Only for business rules, not basic null checks
- **Prefer init over set**: Use `{ get; }` properties when possible
- **Group related properties**: Use nested builders for complex hierarchies

## Validation Patterns

### Custom Validators
```csharp
public static bool ValidateEmail(string email)
{
    return !string.IsNullOrEmpty(email) && email.Contains("@");
}

public static bool ValidateAge(int age)
{
    return age >= 0 && age <= 150;
}
```

### Required Field Strategy
```csharp
// Runtime validation
[BuilderProperty(Required = true)]
public string Name { get; }

// Usage - throws InvalidOperationException if Name not set
var obj = builder.Build(); 
```

### Null Handling
```csharp
// Strict null checking
[BuilderProperty(AllowNull = false)]
public string Name { get; }

// Nullable fields  
[BuilderProperty(AllowNull = true)]
public string? Description { get; }
```

## Migration Strategy

### From Constructor Overloads
**Before**:
```csharp
public DatabaseConfig(string connectionString)
    : this(connectionString, TimeSpan.FromSeconds(30), 100) { }

public DatabaseConfig(string connectionString, TimeSpan timeout)  
    : this(connectionString, timeout, 100) { }

public DatabaseConfig(string connectionString, TimeSpan timeout, int poolSize)
{
    // Implementation
}
```

**After**:
```csharp
[GenerateBuilder]
public class DatabaseConfig
{
    [BuilderProperty(Required = true)]
    public string ConnectionString { get; }
    // Builder handles all combinations
}
```

### From Mutable Objects
**Before**:
```csharp
var config = new DatabaseConfig();
config.ConnectionString = "...";  // Mutable, error-prone
config.Timeout = TimeSpan.FromSeconds(30);
```

**After**:
```csharp
var config = DatabaseConfigBuilder.Create()
    .WithConnectionString("...")
    .WithTimeout(TimeSpan.FromSeconds(30))
    .Build();  // Immutable result
```

## Integration with Legacy Code

### Gradual Adoption
1. **Start with new classes**: Apply `[GenerateBuilder]` to new domain objects
2. **Migrate complex constructors**: Replace parameter-heavy constructors
3. **Convert test builders**: Replace manual test data builders
4. **Standardize configurations**: Use for service configuration objects

### Coexistence Patterns
```csharp
// Support both patterns during transition
public class LegacyClass
{
    // Keep existing constructors
    public LegacyClass(string name) { Name = name; }
    
    // Add builder support
    [BuilderProperty] 
    public string Name { get; }
}
```
---

