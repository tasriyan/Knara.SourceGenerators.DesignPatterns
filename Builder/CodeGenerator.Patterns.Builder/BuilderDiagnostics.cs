using Microsoft.CodeAnalysis;

namespace CodeGenerator.Patterns.Builder;

public static class BuilderDiagnostics
{
    public static readonly DiagnosticDescriptor RequiredPropertyNotSet = new(
        id: "BLD001",
        title: "Required property not set in builder",
        messageFormat: "Required property '{0}' has not been set in the builder",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidValidatorMethod = new(
        id: "BLD002",
        title: "Invalid validator method signature",
        messageFormat: "Validator method '{0}' must be static and return bool with single parameter of type '{1}'",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedPropertyType = new(
        id: "BLD003",
        title: "Unsupported property type for builder",
        messageFormat: "Property type '{0}' is not supported by the builder generator",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}