namespace SourceGenerators.DesignPatterns.Builder;

public class GenerateBuilderAttribute
{
    public bool ValidateOnBuild { get; set; } = true;
    public string? BuilderName { get; set; }
    public bool GenerateWithMethods { get; set; } = true;
    public bool GenerateFromMethod { get; set; } = true;
    public bool GenerateToBuilderMethod { get; set; } = true;
    public BuilderAccessibility Accessibility { get; set; } = BuilderAccessibility.Public;
}

public class BuilderAttribute
{
    public bool ValidateOnBuild { get; set; } = true;
    public string? BuilderName { get; set; }
    public bool GenerateWithMethods { get; set; } = true;
    public bool GenerateFromMethod { get; set; } = true;
    public bool GenerateToBuilderMethod { get; set; } = true;
    public BuilderAccessibility Accessibility { get; set; } = BuilderAccessibility.Public;
}

public class BuilderPropertyAttribute
{
    public bool Required { get; set; }
    public string? ValidatorMethod { get; set; }
    public object? DefaultValue { get; set; }
    public bool IgnoreInBuilder { get; set; }
    public string? CustomSetterName { get; set; }
    public bool AllowNull { get; set; } = true;
}

public class BuilderCollectionAttribute
{
    public string? AddMethodName { get; set; }
    public string? AddRangeMethodName { get; set; }
    public bool GenerateClearMethod { get; set; } = true;
    public bool GenerateCountProperty { get; set; } = true;
}