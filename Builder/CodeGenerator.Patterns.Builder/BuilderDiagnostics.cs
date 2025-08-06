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

    public static readonly DiagnosticDescriptor TypeSymbolNotResolved = new(
        id: "BLD004",
        title: "Type symbol could not be resolved",
        messageFormat: "Unable to resolve type symbol for '{0}'. Builder generation skipped",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor BuilderNameConflict = new(
        id: "BLD005",
        title: "Builder name conflicts with existing type",
        messageFormat: "Builder name '{0}' conflicts with existing type in namespace. Consider using BuilderName attribute for type '{1}'",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NoPropertiesForBuilder = new(
        id: "BLD006",
        title: "No properties found for builder generation",
        messageFormat: "Type '{0}' has no settable properties or properties marked for builder generation",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConflictingPropertyAttributes = new(
        id: "BLD007",
        title: "Conflicting property attributes detected",
        messageFormat: "Property '{0}' has both BuilderProperty and BuilderCollection attributes which may cause conflicts",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor CodeGenerationError = new(
        id: "BLD008",
        title: "Code generation error occurred",
        messageFormat: "Failed to generate builder for type '{0}': {1}",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}