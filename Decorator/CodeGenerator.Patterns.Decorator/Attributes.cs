using System;

namespace CodeGenerator.Patterns.Decorator;

/// <summary>
/// Marks an interface for decorator factory generation.
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public class GenerateDecoratorFactoryAttribute : Attribute
{
    /// <summary>
    /// The base name for the generated factory class. If not specified, uses the interface name without the 'I' prefix.
    /// </summary>
    public string BaseName { get; set; } = string.Empty;
}

/// <summary>
/// Marks a class as a decorator for the interface it implements.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class DecoratorAttribute : Attribute
{
    /// <summary>
    /// The type/name of the decorator. Used to generate the "With{Type}" method name.
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// The order in which this decorator should be applied when using automatic ordering.
    /// Lower numbers are applied first (closer to the core implementation).
    /// </summary>
    public int Order { get; set; } = 0;
}