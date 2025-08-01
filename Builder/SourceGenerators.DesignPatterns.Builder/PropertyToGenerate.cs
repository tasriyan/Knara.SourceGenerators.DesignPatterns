namespace SourceGenerators.DesignPatterns.Builder;

public class PropertyToGenerate
{
    public string Name { get; set; } = "";
    public string TypeName { get; set; } = "";
    public bool IsRequired { get; set; }
    
    public string? ValidatorMethod { get; set; }
    public string? DefaultValue { get; set; }
    public bool IgnoreInBuilder { get; set; }
    public string? CustomSetterName { get; set; }
    public bool AllowNull { get; set; } = true;
    public bool IsCollection { get; set; }
    public bool HasSetter { get; set; }
    public string? CollectionElementType { get; set; }
    public string? AddMethodName { get; set; }
    public bool GenerateClearMethod { get; set; } = true;
    public bool GenerateCountProperty { get; set; } = true;
}