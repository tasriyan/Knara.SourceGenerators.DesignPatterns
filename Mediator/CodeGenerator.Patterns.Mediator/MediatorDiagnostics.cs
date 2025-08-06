using Microsoft.CodeAnalysis;

namespace CodeGenerator.Patterns.Mediator;

public static class MediatorDiagnostics
{
    public static readonly DiagnosticDescriptor MissingRequestName = new(
        id: "MED001",
        title: "Request attribute missing Name property",
        messageFormat: "Request class '{0}' must specify a Name in the [{1}] attribute",
        category: "Mediator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingHandlerName = new(
        id: "MED002",
        title: "Handler attribute missing Name property",
        messageFormat: "Handler class '{0}' must specify a Name in the [{1}] attribute",
        category: "Mediator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor RequestTypeNotFound = new(
        id: "MED003",
        title: "Handler references non-existent request type",
        messageFormat: "Handler '{0}' references RequestType '{1}' which was not found in the compilation",
        category: "Mediator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor HandlerNotFound = new(
        id: "MED004",
        title: "Request has no corresponding handler",
        messageFormat: "Request '{0}' has no corresponding handler. Add a [{1}Handler] class to process this request",
        category: "Mediator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingHandlerMethod = new(
        id: "MED005",
        title: "Handler class missing expected method",
        messageFormat: "Handler class '{0}' should have one of these methods: {1}",
        category: "Mediator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConflictingRequestAttributes = new(
        id: "MED006",
        title: "Multiple request attributes on same class",
        messageFormat: "Class '{0}' has multiple request attributes [{1}]. Only one request attribute per class is allowed",
        category: "Mediator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConflictingHandlerAttributes = new(
        id: "MED007",
        title: "Multiple handler attributes on same class",
        messageFormat: "Class '{0}' has multiple handler attributes [{1}]. Only one handler attribute per class is allowed",
        category: "Mediator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TypeSymbolNotResolved = new(
        id: "MED008",
        title: "Type symbol could not be resolved",
        messageFormat: "Unable to resolve type symbol for '{0}'. Mediator generation skipped",
        category: "Mediator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidResponseType = new(
        id: "MED009",
        title: "Invalid response type specified",
        messageFormat: "Response type '{0}' specified in attribute for '{1}' could not be resolved",
        category: "Mediator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor CodeGenerationError = new(
        id: "MED999",
        title: "Code generation error",
		messageFormat: "Failed to generate mediator code: {0}",
		category: "Mediator",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);
}