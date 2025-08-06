using Microsoft.CodeAnalysis;

namespace CodeGenerator.Patterns.Decorator;

public static class DecoratorDiagnostics
{
	public static readonly DiagnosticDescriptor MissingDecoratorType = new(
		id: "DEC001",
		title: "Decorator attribute missing Type property",
		messageFormat: "Decorator class '{0}' must specify a Type in the [Decorator] attribute",
		category: "Decorator",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor DecoratorNotImplementingInterface = new(
		id: "DEC002",
		title: "Decorator class does not implement target interface",
		messageFormat: "Decorator class '{0}' must implement interface '{1}' marked with [GenerateDecoratorFactory]",
		category: "Decorator",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor InvalidDecoratorConstructor = new(
		id: "DEC003",
		title: "Invalid decorator constructor signature",
		messageFormat: "Decorator class '{0}' must have a constructor with '{1}' as the first parameter",
		category: "Decorator",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor FactoryNameConflict = new(
		id: "DEC004",
		title: "Decorator factory name conflicts with existing type",
		messageFormat: "Generated factory name '{0}DecoratorFactory' conflicts with existing type. Consider using BaseName attribute property",
		category: "Decorator",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor NoValidDecorators = new(
		id: "DEC005",
		title: "No valid decorators found for interface",
		messageFormat: "Interface '{0}' is marked for decorator factory generation but no valid decorators were found",
		category: "Decorator",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor InterfaceSymbolNotResolved = new(
		id: "DEC006",
		title: "Interface symbol could not be resolved",
		messageFormat: "Unable to resolve interface symbol for '{0}'. Decorator factory generation skipped",
		category: "Decorator",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor CodeGenerationError = new(
		id: "DEC999",
		title: "Code generation error",
		messageFormat: "Failed to generate decorator factory for interface '{0}': {1}",
		category: "Decorator",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);
}