# Declarative Mediator Generator

A C# source generator that retrofits existing services with the mediator pattern using declarative attributes. 
Designed for gradual CQRS adoption in legacy codebases.
Generates mediator infrastructure at compile-time using Roslyn analysis.

## What It Does

This generator creates mediator boilerplate for two distinct scenarios:

### 1. CQRS-Style (New Code)
Clean separation of commands and queries with dedicated request/handler classes:

```csharp
[Query(Name = "GetUserQuery", ResponseType = typeof(User))]
public record GetUserRequest(int UserId);

[QueryHandler(Name="GetUserHandler", RequestType = typeof(GetUserQuery))]
public class GetUserService(IUserRepository repository) 
{
    public async Task<User> GetAsync(GetUserRequest request, CancellationToken ct) { ... }
}
```

### 2. Legacy Retrofitting (Existing Code)
Method-level attributes that wrap existing services without modification:

```csharp
public class LegacyUserService
{
    [RequestHandler(Name="GetUserHandler")]
    public async Task<User> GetUserAsync(int userId, CancellationToken ct) { ... }
    
    [RequestHandler(Name="CreateUserHandler")]  
    public async Task AddNewUserAsync(NewUserModel model, CancellationToken ct) { ... }
    
    [RequestHandler(Name="LegacyUserUpdateUserHandler")]
	public async Task<User> UpdateAsync(int userId, string email, string firstName, DateTime updateDate, CancellationToken cancellationToken = default) { ... }
}
```

Both generate the same mediator infrastructure but serve different migration strategies.

## Quick Start
Add the source generator to your project:
```bash
<ItemGroup> <ProjectReference Include="path/to/Knara.SourceGenerators.DesignPatterns.Mediator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" /> </ItemGroup>
```

Or via NuGet (when published):
```bash
dotnet add package Knara.SourceGenerators.DesignPatterns.Mediator
```
If you are using the generator in .net 4.+ projects, refer to [this guide](../dotnet-legacy-guide.md) for additional steps.

## Usage Patterns

### CQRS-Style Usage
```csharp
// Generated: GetUserQuery : IQuery<User>
var user = await mediator.Send(new GetUserQuery { UserId = 123 });

// Generated: CreateUserCommand : ICommand<bool>  
var success = await mediator.Send(new CreateUserCommand { Email = "test@example.com" });
```

### Legacy Retrofitting Usage
```csharp
// Generated: GetUserRequest : IRequest<User>
var user = await mediator.Send(new GetUserRequest { UserId = 123 });

// Generated: CreateUserRequest : IRequest
await mediator.Send(new CreateUserRequest { Email = "test@example.com" });
```

## Registration

```csharp
// In your Startup.cs or Program.cs if using .NET 6+ 
services.AddDeclarativeMediator(); // Auto-registers all handlers and services
```
If you are using the generator in .net 4.+ projects, you need to manually register the generated types and mediator.

## When to Use Each Pattern

### CQRS-Style (`[Query]`/`[Command]`)
**Use for:**
- New feature development
- Clean architectural boundaries
- Complex business domains
- Teams adopting CQRS principles

### Legacy Retrofitting (`[RequestHandler]`)
**Use for:**
- Existing codebases with established patterns
- Gradual mediator adoption
- Risk-averse migration strategies
- Mixed architectural approaches during transition

## Pros

✅ **Dual approach** - Supports both clean CQRS and pragmatic retrofitting  
✅ **Compile-time generation** - No runtime reflection, better performance than MediatR  
✅ **Non-breaking** - Legacy services remain callable directly during transition  
✅ **Type safety** - All request routing resolved at compile-time  
✅ **Minimal friction** - Add attributes to existing code without restructuring

## Cons

❌ **Complexity** - Two different patterns in same codebase can confuse teams  
❌ **Type proliferation** - Generates many request/handler classes  
❌ **Generated code debugging** - Harder to troubleshoot than explicit implementations  
❌ **Limited pipeline** - Missing MediatR's rich behavior/middleware ecosystem  
❌ **Learning curve** - Teams need to understand both patterns and when to use each

## Performance

**Faster than MediatR** due to compile-time generation:
- Direct method calls vs reflection
- Pattern matching vs runtime type resolution
- `GetRequiredService<ConcreteType>()` vs generic service location

## Limitations

**This is NOT a drop-in MediatR replacement.** Missing:
- Pipeline behaviors and middleware
- Request preprocessing/postprocessing
- Polymorphic request handling
- Validation pipeline integration
- Advanced error handling patterns
- Request/response decorators

For full-featured mediator requirements, use [MediatR](https://github.com/jbogard/MediatR) directly.

## Migration Strategy

**Recommended approach:**

1. **Start with legacy pattern** - Add `[RequestHandler]` to existing methods
2. **Establish mediator usage** - Route new features through generated mediator
3. **Introduce CQRS gradually** - Use `[Query]`/`[Command]` for new bounded contexts
4. **Migrate incrementally** - Convert legacy handlers to CQRS as business needs require
5. **Consider full MediatR** - When you need advanced pipeline features

## Use Cases

### ✅ Good Fit
- **Legacy modernization** with risk constraints
- **Mixed architectural periods** during large migrations
- **Performance-sensitive** applications where reflection overhead matters
- **Simple request/response** patterns without complex pipelines
- **Teams learning** mediator patterns incrementally

### ❌ Poor Fit
- **Greenfield projects** (just use MediatR)
- **Complex pipeline requirements** (validation, caching, logging, etc.)
- **Heavy behavior composition** needs
- **Small codebases** where generated complexity outweighs benefits
- **Teams wanting industry-standard** patterns

## Code Generation Output

For each pattern, the generator creates:
- **Request classes** implementing appropriate interfaces
- **Handler classes** wrapping your service methods
- **Mediator implementation** with compile-time request routing
- **DI registration** extensions for all generated types

Example generated structure:
```
├── GetUserQuery.Request.g.cs          # CQRS request class
├── GetUserHandler.Handler.g.cs        # CQRS handler wrapper  
├── GetUserRequest.Request.g.cs        # Legacy request class
├── GetUserHandler.Handler.g.cs        # Legacy handler wrapper
├── GeneratedMediator.g.cs             # Unified mediator implementation
└── MediatorDIExtensions.g.cs          # Service registration
```

## Bottom Line

Consider this a stepping stone toward full mediator adoption rather than a permanent architectural decision.

---
