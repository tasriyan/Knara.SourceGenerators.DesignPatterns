using System;

namespace CodeGenerator.Patterns.Mediator;

// CQRS-style Request pattern attributes (EXISTING)
[AttributeUsage(AttributeTargets.Class)]
public class QueryAttribute : Attribute
{
    public string Name { get; set; } = "";
    public Type? ResponseType { get; set; }
}

[AttributeUsage(AttributeTargets.Class)]
public class CommandAttribute : Attribute
{
    public string Name { get; set; } = "";
    public Type? ResponseType { get; set; }
}

[AttributeUsage(AttributeTargets.Class)]
public class StreamQueryAttribute : Attribute
{
    public string Name { get; set; } = "";
    public Type? ResponseType { get; set; }
}

// CQRS-style Handler pattern attributes (EXISTING)
[AttributeUsage(AttributeTargets.Class)]
public class QueryHandlerAttribute : Attribute
{
    public string Name { get; set; } = "";
    public Type? RequestType { get; set; }
}

[AttributeUsage(AttributeTargets.Class)]
public class CommandHandlerAttribute : Attribute
{
    public string Name { get; set; } = "";
    public Type? RequestType { get; set; }
    public Type? PublisherType { get; set; }
}

[AttributeUsage(AttributeTargets.Class)]
public class NotificationHandlerAttribute : Attribute
{
    public string Name { get; set; } = "";
    public Type? EventType { get; set; }
}

[AttributeUsage(AttributeTargets.Class)]
public class StreamQueryHandlerAttribute : Attribute
{
    public string Name { get; set; } = "";
    public Type? RequestType { get; set; }
}

// NEW: Legacy method-level pattern (MediatR-style)
[AttributeUsage(AttributeTargets.Method)]
public class RequestHandlerAttribute : Attribute
{
    public string Name { get; set; } = "";
}
