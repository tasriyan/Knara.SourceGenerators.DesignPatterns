using System;

namespace SourceGenerators.DesignPatterns.Builder;

[AttributeUsage(AttributeTargets.Class)]
public class GenerateBuilderAttribute : Attribute
{
    public bool ValidateOnBuild { get; set; } = true;
    public string? BuilderName { get; set; }
    public bool GenerateWithMethods { get; set; } = true;
    public bool GenerateFromMethod { get; set; } = true;
    public bool GenerateToBuilderMethod { get; set; } = true;
    public BuilderAccessibility Accessibility { get; set; } = BuilderAccessibility.Public;
}

[AttributeUsage(AttributeTargets.Property)]
public class BuilderPropertyAttribute : Attribute
{
    public bool Required { get; set; }
    public string? ValidatorMethod { get; set; }
    public object? DefaultValue { get; set; }
    public bool IgnoreInBuilder { get; set; }
    public string? CustomSetterName { get; set; }
    public bool AllowNull { get; set; } = true;
}

[AttributeUsage(AttributeTargets.Property)]
public class BuilderCollectionAttribute : Attribute
{
    public string? AddMethodName { get; set; }
    public string? AddRangeMethodName { get; set; }
    public bool GenerateClearMethod { get; set; } = true;
    public bool GenerateCountProperty { get; set; } = true;
}

public enum BuilderAccessibility
{
    Public,
    Internal,
    Private
}