using System.Collections.Generic;

namespace SourceGenerators.DesignPatterns.Decorator;

public class InterfaceToDecorate
{
    public string Name { get; set; } = "";
    public string Namespace { get; set; } = "";
    public List<MethodToDecorate> Methods { get; set; } = new();
    public string BaseName { get; set; } = "";
    public bool GenerateFactory { get; set; } = true;
    public bool GenerateAsyncSupport { get; set; } = true;
    public DecoratorAccessibility Accessibility { get; set; } = DecoratorAccessibility.Public;
}
