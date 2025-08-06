using Microsoft.CodeAnalysis;

namespace CodeGenerator.Patterns.Singleton;

public class DiagnosticInfo
{
	public DiagnosticDescriptor Descriptor { get; }
	public Location Location { get; }
	public object[] Args { get; }

	public DiagnosticInfo(DiagnosticDescriptor descriptor, Location location, params object[] args)
	{
		Descriptor = descriptor;
		Location = location;
		Args = args ?? [];
	}
}

public static class SingletonDiagnostics
{
    public static readonly DiagnosticDescriptor ClassNotPartial = new(
        id: "SIN001",
        title: "Class with Singleton attribute must be partial",
        messageFormat: "Class '{0}' has [Singleton] attribute but is not declared as partial. Add 'partial' keyword to the class declaration",
        category: "Singleton",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingFactoryMethod = new(
        id: "SIN002",
        title: "Factory method not found",
        messageFormat: "Class '{0}' has UseFactory=true but factory method '{1}' was not found. Add a static method that returns an instance of '{0}'",
        category: "Singleton",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidFactoryMethodSignature = new(
        id: "SIN003",
        title: "Invalid factory method signature",
        messageFormat: "Factory method '{0}' in class '{1}' must be static, parameterless, and return an instance of '{1}'",
        category: "Singleton",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TypeSymbolNotResolved = new(
        id: "SIN004",
        title: "Type symbol could not be resolved",
        messageFormat: "Unable to resolve type symbol for '{0}'. Singleton generation skipped",
        category: "Singleton",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConflictingConfiguration = new(
        id: "SIN005",
        title: "Conflicting singleton configuration",
        messageFormat: "Class '{0}' has conflicting singleton configuration: {1}",
        category: "Singleton",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor GenericConstraintWarning = new(
        id: "SIN006",
        title: "Generic singleton with specific constraints",
        messageFormat: "Generic singleton '{0}' with '{1}' strategy may have performance implications with constraint '{2}'",
        category: "Singleton",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor CodeGenerationError = new(
        id: "SIN999",
        title: "Code generation error",
		messageFormat: "Failed to generate singleton for class '{0}': {1}",
		category: "Singleton",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);
}