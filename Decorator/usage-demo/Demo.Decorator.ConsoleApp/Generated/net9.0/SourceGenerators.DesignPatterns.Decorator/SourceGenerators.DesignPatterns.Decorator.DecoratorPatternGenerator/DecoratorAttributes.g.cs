using System;

namespace Demo.Generator;

[AttributeUsage(AttributeTargets.Interface)]
public class GenerateDecoratorsAttribute : Attribute
{
    public string? BaseName { get; set; }
    public bool GenerateFactory { get; set; } = true;
    public bool GenerateAsyncSupport { get; set; } = true;
    public DecoratorAccessibility Accessibility { get; set; } = DecoratorAccessibility.Public;
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class DecoratorAttribute : Attribute
{
    public DecoratorType Type { get; set; }
    public string? Name { get; set; }
    public string? LoggerProperty { get; set; }
    public string? CacheKeyFormat { get; set; }
    public int CacheExpirationMinutes { get; set; } = 60;
    public string? ValidationMethod { get; set; }
    public int RetryAttempts { get; set; } = 3;
    public string? MetricName { get; set; }
}

public enum DecoratorAccessibility
{
    Public,
    Internal,
    Private
}

public enum DecoratorType
{
    Logging,
    Caching,
    Validation,
    Retry,
    Performance,
    Authorization,
    CircuitBreaker
}