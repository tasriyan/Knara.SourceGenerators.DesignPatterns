using System;

namespace CodeGenerator.Patterns.Mediator;

// Handler attributes
[AttributeUsage(AttributeTargets.Class)]
public class QueryHandlerAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class)]
public class CommandHandlerAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class)]
public class NotificationHandlerAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class)]
public class StreamQueryHandlerAttribute : Attribute { }
