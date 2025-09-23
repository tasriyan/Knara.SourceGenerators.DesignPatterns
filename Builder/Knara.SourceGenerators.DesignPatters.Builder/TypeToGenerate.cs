using System.Collections.Generic;

namespace Knara.SourceGenerators.DesignPatters.Builder;

public class TypeToGenerate
{
    public string Name { get; set; } = "";
    public string Namespace { get; set; } = "";
    public bool IsRecord { get; set; }
    public List<PropertyToGenerate> Properties { get; set; } = new();
    public string BuilderName { get; set; } = "";
    public bool ValidateOnBuild { get; set; } = true;
    public bool GenerateWithMethods { get; set; } = true;
    public bool GenerateFromMethod { get; set; } = true;
    public BuilderAccessibility Accessibility { get; set; } = BuilderAccessibility.Public;
}

public enum BuilderAccessibility
{
    Public,
    Internal,
    Private
}