using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace CodeGenerator.Patterns.Mediator;


[Generator]
public class DeclarativeMediatorGenerator : IIncrementalGenerator
{
    private const string MediatorAttributes = @"
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CodeGenerator.Patterns.Mediator
{
    // CQRS-style Request pattern attributes (EXISTING)
    [AttributeUsage(AttributeTargets.Class)]
    public class QueryAttribute : Attribute
    {
        public string Name { get; set; } = """";
        public Type? ResponseType { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class CommandAttribute : Attribute
    {
        public string Name { get; set; } = """";
        public Type? ResponseType { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class StreamQueryAttribute : Attribute
    {
        public string Name { get; set; } = """";
        public Type? ResponseType { get; set; }
    }

    // CQRS-style Handler pattern attributes (EXISTING)
    [AttributeUsage(AttributeTargets.Class)]
    public class QueryHandlerAttribute : Attribute
    {
        public string Name { get; set; } = """";
        public Type? RequestType { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class CommandHandlerAttribute : Attribute
    {
        public string Name { get; set; } = """";
        public Type? RequestType { get; set; }
        public Type? PublisherType { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class NotificationHandlerAttribute : Attribute
    {
        public string Name { get; set; } = """";
        public Type? EventType { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class StreamQueryHandlerAttribute : Attribute
    {
        public string Name { get; set; } = """";
        public Type? RequestType { get; set; }
    }

    // NEW: Legacy method-level pattern (MediatR-style)
    [AttributeUsage(AttributeTargets.Method)]
    public class RequestHandlerAttribute : Attribute
    {
        public string Name { get; set; } = """";
    }

    // CQRS-style interfaces (EXISTING)
    public interface IQuery<out TResponse> { }
    public interface ICommand { }
    public interface ICommand<out TResponse> { }
    public interface IStreamQuery<out TResponse> { }

    public interface IQueryHandler<in TQuery, TResponse> where TQuery : IQuery<TResponse>
    {
        Task<TResponse> Handle(TQuery query, CancellationToken cancellationToken = default);
    }

    public interface ICommandHandler<in TCommand> where TCommand : ICommand
    {
        Task Handle(TCommand command, CancellationToken cancellationToken = default);
    }

    public interface ICommandHandler<in TCommand, TResponse> where TCommand : ICommand<TResponse>
    {
        Task<TResponse> Handle(TCommand command, CancellationToken cancellationToken = default);
    }

    public interface IStreamQueryHandler<in TQuery, TResponse> where TQuery : IStreamQuery<TResponse>
    {
	    IAsyncEnumerable<TResponse> Handle(TQuery query, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default);
    }

    // NEW: Basic MediatR-style interfaces for legacy retrofitting
    public interface IRequest { }
    public interface IRequest<out TResponse> { }

    public interface IRequestHandler<in TRequest> where TRequest : IRequest
    {
        Task Handle(TRequest request, CancellationToken cancellationToken = default);
    }

    public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken = default);
    }

    // Extended mediator interface supporting both patterns
    public interface IMediator
    {
        // CQRS-style methods (EXISTING)
        Task<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);
        Task Send(ICommand command, CancellationToken cancellationToken = default);
        Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);
        Task Publish<TEvent>(TEvent eventObj, CancellationToken cancellationToken = default);
        IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamQuery<TResponse> query, CancellationToken cancellationToken = default);
        
        // NEW: MediatR-style methods for legacy support
        Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
        Task Send(IRequest request, CancellationToken cancellationToken = default);
    }
}";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Add the mediator attribute interfaces
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource("MediatorAttributes.g.cs", SourceText.From(MediatorAttributes, Encoding.UTF8));
        });

        // EXISTING: Get request classes (Query, Command, StreamQuery)
        var requestClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsRequestClass(s),
                transform: static (ctx, _) => GetRequestInfo(ctx))
            .Where(static m => m is not null);

        // EXISTING: Get handler classes (QueryHandler, CommandHandler, NotificationHandler, StreamQueryHandler)
        var handlerClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsHandlerClass(s),
                transform: static (ctx, _) => GetHandlerInfo(ctx))
            .Where(static m => m is not null);

        // NEW: Get legacy method handlers ([RequestHandler] on methods)
        var legacyMethodHandlers = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsLegacyMethodHandler(s),
                transform: static (ctx, _) => GetLegacyMethodInfo(ctx))
            .Where(static m => m is not null);

        // Combine all sources and generate
        var combined = requestClasses.Collect()
            .Combine(handlerClasses.Collect())
            .Combine(legacyMethodHandlers.Collect());

        context.RegisterSourceOutput(combined, static (spc, source) => 
            GenerateMediatorCode(spc, source.Left.Left, source.Left.Right, source.Right));
    }

    private static bool IsRequestClass(SyntaxNode node)
    {
        return (node is ClassDeclarationSyntax cls && cls.AttributeLists.Count > 0) ||
               (node is RecordDeclarationSyntax rec && rec.AttributeLists.Count > 0);
    }

    // NEW: Detect methods with [RequestHandler] attribute
    private static bool IsLegacyMethodHandler(SyntaxNode node)
    {
        if (node is not MethodDeclarationSyntax method) return false;
        
        return method.AttributeLists
            .SelectMany(list => list.Attributes)
            .Any(attr => attr.Name.ToString().Contains("RequestHandler"));
    }
    
    private static bool IsHandlerClass(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax cls && cls.AttributeLists.Count > 0;
    }
    
       // NEW: Extract legacy method handler information
    private static LegacyMethodInfo? GetLegacyMethodInfo(GeneratorSyntaxContext context)
    {
        var method = (MethodDeclarationSyntax)context.Node;
        var model = context.SemanticModel;
        var methodSymbol = model.GetDeclaredSymbol(method);

        if (methodSymbol == null) return null;

        // Get the RequestHandler attribute
        var requestHandlerAttr = methodSymbol.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name == "RequestHandlerAttribute");

        if (requestHandlerAttr == null) return null;

        var handlerName = GetAttributeStringValue(requestHandlerAttr, "Name");
        if (string.IsNullOrEmpty(handlerName))
        {
            // Generate default name if not provided
            handlerName = $"{methodSymbol.ContainingType.Name}_{methodSymbol.Name}_Handler";
        }

        // Analyze method signature
        var parameters = new List<ParameterInfo>();
        var hasReturnType = !methodSymbol.ReturnsVoid && methodSymbol.ReturnType.Name != "Task";
        string? returnType = null;

        // Extract return type for Task<T>
        if (methodSymbol.ReturnType is INamedTypeSymbol namedReturn && 
            namedReturn.IsGenericType && 
            namedReturn.Name == "Task" &&
            namedReturn.TypeArguments.Length == 1)
        {
            hasReturnType = true;
            returnType = namedReturn.TypeArguments[0].ToDisplayString();
        }

        // Extract parameters (skip CancellationToken)
        foreach (var param in methodSymbol.Parameters)
        {
            if (param.Type.Name == "CancellationToken") continue;
            
            parameters.Add(new ParameterInfo(
                param.Type.ToDisplayString(),
                param.Name,
                GetPropertyInitializerFromType(param.Type, param.NullableAnnotation)
            ));
        }

        var requestName = handlerName.Replace("Handler", "Request").Replace("_", "");

        return new LegacyMethodInfo(
            methodSymbol.ContainingType.Name,
            methodSymbol.Name,
            handlerName,
            requestName,
            parameters,
            hasReturnType,
            returnType,
            methodSymbol.ContainingNamespace?.ToDisplayString() ?? ""
        );
    }

    private static RequestInfoResult? GetRequestInfo(GeneratorSyntaxContext context)
    {
        var model = context.SemanticModel;
        INamedTypeSymbol? symbol = null;
        bool isRecord = false;
        SyntaxNode node = context.Node;
        var diagnostics = new List<DiagnosticInfo>();

        // Handle both class and record declarations
        if (context.Node is ClassDeclarationSyntax classDecl)
        {
            symbol = model.GetDeclaredSymbol(classDecl);
            isRecord = false;
            node = classDecl;
        }
        else if (context.Node is RecordDeclarationSyntax recordDecl)
        {
            symbol = model.GetDeclaredSymbol(recordDecl);
            isRecord = true;
            node = recordDecl;
        }

        if (symbol == null) 
        {
            var location = node.GetLocation();
            diagnostics.Add(new DiagnosticInfo(
                MediatorDiagnostics.TypeSymbolNotResolved,
                location,
                GetNodeIdentifier(node)));
            return new RequestInfoResult(null, diagnostics);
        }

        // Check for conflicting request attributes
        var requestAttributes = GetRequestAttributes(symbol);
        if (requestAttributes.Count > 1)
        {
            var location = node.GetLocation();
            var attributeNames = string.Join(", ", requestAttributes.Select(a => a.AttributeClass?.Name ?? "Unknown"));
            diagnostics.Add(new DiagnosticInfo(
                MediatorDiagnostics.ConflictingRequestAttributes,
                location,
                symbol.Name,
                attributeNames));
            return new RequestInfoResult(null, diagnostics);
        }

        // Extract properties from the class/record
        var properties = ExtractProperties(symbol, isRecord);

        var queryAttr = GetAttribute(symbol, "QueryAttribute");
        if (queryAttr != null)
        {
            var name = GetAttributeStringValue(queryAttr, "Name");
            if (string.IsNullOrEmpty(name))
            {
                var location = node.GetLocation();
                diagnostics.Add(new DiagnosticInfo(
                    MediatorDiagnostics.MissingRequestName,
                    location,
                    symbol.Name,
                    "Query"));
                return new RequestInfoResult(null, diagnostics);
            }
            var responseType = GetAttributeTypeValue(queryAttr, "ResponseType");
            var requestInfo = new RequestInfo(symbol.Name, name, RequestType.Query, responseType, symbol.ContainingNamespace?.ToDisplayString() ?? "", properties, isRecord);
            return new RequestInfoResult(requestInfo, diagnostics);
        }

        var commandAttr = GetAttribute(symbol, "CommandAttribute");
        if (commandAttr != null)
        {
            var name = GetAttributeStringValue(commandAttr, "Name");
            if (string.IsNullOrEmpty(name))
            {
                var location = node.GetLocation();
                diagnostics.Add(new DiagnosticInfo(
                    MediatorDiagnostics.MissingRequestName,
                    location,
                    symbol.Name,
                    "Command"));
                return new RequestInfoResult(null, diagnostics);
            }
            var responseType = GetAttributeTypeValue(commandAttr, "ResponseType");
            var requestInfo = new RequestInfo(symbol.Name, name, RequestType.Command, responseType, symbol.ContainingNamespace?.ToDisplayString() ?? "", properties, isRecord);
            return new RequestInfoResult(requestInfo, diagnostics);
        }

        var streamQueryAttr = GetAttribute(symbol, "StreamQueryAttribute");
        if (streamQueryAttr != null)
        {
            var name = GetAttributeStringValue(streamQueryAttr, "Name");
            if (string.IsNullOrEmpty(name))
            {
                var location = node.GetLocation();
                diagnostics.Add(new DiagnosticInfo(
                    MediatorDiagnostics.MissingRequestName,
                    location,
                    symbol.Name,
                    "StreamQuery"));
                return new RequestInfoResult(null, diagnostics);
            }
            var responseType = GetAttributeTypeValue(streamQueryAttr, "ResponseType");
            var requestInfo = new RequestInfo(symbol.Name, name, RequestType.StreamQuery, responseType, symbol.ContainingNamespace?.ToDisplayString() ?? "", properties, isRecord);
            return new RequestInfoResult(requestInfo, diagnostics);
        }

        return null;
    }

    private static List<PropertyInfo> ExtractProperties(INamedTypeSymbol symbol, bool isRecord)
    {
        var properties = new List<PropertyInfo>();

        if (isRecord)
        {
            // For records, extract primary constructor parameters
            var primaryConstructor = symbol.Constructors.FirstOrDefault(c => c.Parameters.Length > 0);
            if (primaryConstructor != null)
            {
                foreach (var parameter in primaryConstructor.Parameters)
                {
                    var typeName = parameter.Type.ToDisplayString();
                    var propertyName = char.ToUpper(parameter.Name[0]) + parameter.Name.Substring(1); // Convert to PascalCase
                    var initializer = GetPropertyInitializerFromType(parameter.Type, parameter.NullableAnnotation);
                    
                    properties.Add(new PropertyInfo(typeName, propertyName, initializer));
                }
            }
        }
        else
        {
            // For classes, extract public properties
            foreach (var member in symbol.GetMembers())
            {
                if (member is IPropertySymbol property && property.DeclaredAccessibility == Accessibility.Public)
                {
                    var typeName = property.Type.ToDisplayString();
                    var propertyName = property.Name;
                    var initializer = GetPropertyInitializer(property);
                    
                    properties.Add(new PropertyInfo(typeName, propertyName, initializer));
                }
            }
        }
        
        return properties;
    }

    private static string GetPropertyInitializerFromType(ITypeSymbol type, NullableAnnotation nullableAnnotation)
    {
        // Handle strings
        if (type.SpecialType == SpecialType.System_String)
        {
            return " = \"\";";
        }
        
        // Don't add initializers for value types (int, bool, DateTime, etc.)
        if (type.IsValueType)
        {
            return "";
        }
        
        // For nullable reference types, don't add initializer
        if (type.CanBeReferencedByName && nullableAnnotation == NullableAnnotation.Annotated)
        {
            return "";
        }
        
        // For non-nullable reference types (excluding string which we handled above), add null!
        if (type.IsReferenceType && nullableAnnotation != NullableAnnotation.Annotated)
        {
            return " = null!;";
        }
        
        return "";
    }
    
    private static string GetPropertyInitializer(IPropertySymbol property)
    {
        // Handle strings
        if (property.Type.SpecialType == SpecialType.System_String)
        {
            return " = \"\";";
        }
        
        // Don't add initializers for value types (int, bool, DateTime, etc.)
        if (property.Type.IsValueType)
        {
            return "";
        }
        
        // For nullable reference types, don't add initializer
        if (property.Type.CanBeReferencedByName && property.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return "";
        }
        
        // For non-nullable reference types (excluding string which we handled above), add null!
        if (property.Type.IsReferenceType && property.NullableAnnotation != NullableAnnotation.Annotated)
        {
            return " = null!;";
        }
        
        return "";
    }

    private static HandlerInfoResult? GetHandlerInfo(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var model = context.SemanticModel;
        var symbol = model.GetDeclaredSymbol(classDecl);
        var diagnostics = new List<DiagnosticInfo>();

        if (symbol == null) 
        {
            var location = classDecl.GetLocation();
            diagnostics.Add(new DiagnosticInfo(
                MediatorDiagnostics.TypeSymbolNotResolved,
                location,
                classDecl.Identifier.Text));
            return new HandlerInfoResult(null, diagnostics);
        }

        // Check for conflicting handler attributes
        var handlerAttributes = GetHandlerAttributes(symbol);
        if (handlerAttributes.Count > 1)
        {
            var location = classDecl.GetLocation();
            var attributeNames = string.Join(", ", handlerAttributes.Select(a => a.AttributeClass?.Name ?? "Unknown"));
            diagnostics.Add(new DiagnosticInfo(
                MediatorDiagnostics.ConflictingHandlerAttributes,
                location,
                symbol.Name,
                attributeNames));
            return new HandlerInfoResult(null, diagnostics);
        }

        var queryHandlerAttr = GetAttribute(symbol, "QueryHandlerAttribute");
        if (queryHandlerAttr != null)
        {
            var name = GetAttributeStringValue(queryHandlerAttr, "Name");
            if (string.IsNullOrEmpty(name))
            {
                var location = classDecl.GetLocation();
                diagnostics.Add(new DiagnosticInfo(
                    MediatorDiagnostics.MissingHandlerName,
                    location,
                    symbol.Name,
                    "QueryHandler"));
                return new HandlerInfoResult(null, diagnostics);
            }
            var requestType = GetAttributeTypeValue(queryHandlerAttr, "RequestType");
            var method = FindMethod(symbol, new[] { "GetAsync", "QueryAsync", "ExecuteAsync" });
            
            // Check if method exists
            if (method == null)
            {
                var location = classDecl.GetLocation();
                diagnostics.Add(new DiagnosticInfo(
                    MediatorDiagnostics.MissingHandlerMethod,
                    location,
                    symbol.Name,
                    "GetAsync, QueryAsync, ExecuteAsync"));
            }
            
            var handlerInfo = new HandlerInfo(symbol.Name, name, HandlerType.Query, requestType, null, method, symbol.ContainingNamespace?.ToDisplayString() ?? "");
            return new HandlerInfoResult(handlerInfo, diagnostics);
        }

        var commandHandlerAttr = GetAttribute(symbol, "CommandHandlerAttribute");
        if (commandHandlerAttr != null)
        {
            var name = GetAttributeStringValue(commandHandlerAttr, "Name");
            if (string.IsNullOrEmpty(name))
            {
                var location = classDecl.GetLocation();
                diagnostics.Add(new DiagnosticInfo(
                    MediatorDiagnostics.MissingHandlerName,
                    location,
                    symbol.Name,
                    "CommandHandler"));
                return new HandlerInfoResult(null, diagnostics);
            }
            var requestType = GetAttributeTypeValue(commandHandlerAttr, "RequestType");
            var publisherType = GetAttributeTypeValue(commandHandlerAttr, "PublisherType");
            var method = FindMethod(symbol, new[] { "CreateAsync", "UpdateAsync", "DeleteAsync", "ExecuteAsync", "ProcessAsync" });
            
            // Check if method exists
            if (method == null)
            {
                var location = classDecl.GetLocation();
                diagnostics.Add(new DiagnosticInfo(
                    MediatorDiagnostics.MissingHandlerMethod,
                    location,
                    symbol.Name,
                    "CreateAsync, UpdateAsync, DeleteAsync, ExecuteAsync, ProcessAsync"));
            }
            
            var handlerInfo = new HandlerInfo(symbol.Name, name, HandlerType.Command, requestType, publisherType, method, symbol.ContainingNamespace?.ToDisplayString() ?? "");
            return new HandlerInfoResult(handlerInfo, diagnostics);
        }

        var notificationHandlerAttr = GetAttribute(symbol, "NotificationHandlerAttribute");
        if (notificationHandlerAttr != null)
        {
            var name = GetAttributeStringValue(notificationHandlerAttr, "Name");
            if (string.IsNullOrEmpty(name))
            {
                var location = classDecl.GetLocation();
                diagnostics.Add(new DiagnosticInfo(
                    MediatorDiagnostics.MissingHandlerName,
                    location,
                    symbol.Name,
                    "NotificationHandler"));
                return new HandlerInfoResult(null, diagnostics);
            }
            var eventType = GetAttributeTypeValue(notificationHandlerAttr, "EventType");
            var method = FindMethod(symbol, new[] { "ProcessAsync", "HandleAsync", "ExecuteAsync" });
            
            // Check if method exists
            if (method == null)
            {
                var location = classDecl.GetLocation();
                diagnostics.Add(new DiagnosticInfo(
                    MediatorDiagnostics.MissingHandlerMethod,
                    location,
                    symbol.Name,
                    "ProcessAsync, HandleAsync, ExecuteAsync"));
            }
            
            var handlerInfo = new HandlerInfo(symbol.Name, name, HandlerType.Notification, eventType, null, method, symbol.ContainingNamespace?.ToDisplayString() ?? "");
            return new HandlerInfoResult(handlerInfo, diagnostics);
        }
        
        var streamQueryHandlerAttr = GetAttribute(symbol, "StreamQueryHandlerAttribute");
        if (streamQueryHandlerAttr != null)
        {
            var name = GetAttributeStringValue(streamQueryHandlerAttr, "Name");
            if (string.IsNullOrEmpty(name))
            {
                var location = classDecl.GetLocation();
                diagnostics.Add(new DiagnosticInfo(
                    MediatorDiagnostics.MissingHandlerName,
                    location,
                    symbol.Name,
                    "StreamQueryHandler"));
                return new HandlerInfoResult(null, diagnostics);
            }
            var requestType = GetAttributeTypeValue(streamQueryHandlerAttr, "RequestType");
            var method = FindMethod(symbol, new[] { "GetAsync", "HandleAsync", "ExecuteAsync" });
            
            // Check if method exists
            if (method == null)
            {
                var location = classDecl.GetLocation();
                diagnostics.Add(new DiagnosticInfo(
                    MediatorDiagnostics.MissingHandlerMethod,
                    location,
                    symbol.Name,
                    "GetAsync, HandleAsync, ExecuteAsync"));
            }
            
            var handlerInfo = new HandlerInfo(symbol.Name, name, HandlerType.StreamQuery, requestType, null, method, symbol.ContainingNamespace?.ToDisplayString() ?? "");
            return new HandlerInfoResult(handlerInfo, diagnostics);
        }

        return null;
    }

    // Helper methods for diagnostics
    private static List<AttributeData> GetRequestAttributes(INamedTypeSymbol symbol)
    {
        return symbol.GetAttributes()
            .Where(attr => attr.AttributeClass?.Name is "QueryAttribute" or "CommandAttribute" or "StreamQueryAttribute")
            .ToList();
    }

    private static List<AttributeData> GetHandlerAttributes(INamedTypeSymbol symbol)
    {
        return symbol.GetAttributes()
            .Where(attr => attr.AttributeClass?.Name is "QueryHandlerAttribute" or "CommandHandlerAttribute" or "NotificationHandlerAttribute" or "StreamQueryHandlerAttribute")
            .ToList();
    }

    private static string GetNodeIdentifier(SyntaxNode node)
    {
        return node switch
        {
            ClassDeclarationSyntax cls => cls.Identifier.Text,
            RecordDeclarationSyntax rec => rec.Identifier.Text,
            _ => "Unknown"
        };
    }

    private static AttributeData? GetAttribute(INamedTypeSymbol symbol, string attributeName)
    {
        return symbol.GetAttributes().FirstOrDefault(attr => attr.AttributeClass?.Name == attributeName);
    }

    private static string GetAttributeStringValue(AttributeData attribute, string propertyName)
    {
        var namedArg = attribute.NamedArguments.FirstOrDefault(kv => kv.Key == propertyName);
        return namedArg.Value.Value?.ToString() ?? "";
    }

    private static string? GetAttributeTypeValue(AttributeData attribute, string propertyName)
    {
        var namedArg = attribute.NamedArguments.FirstOrDefault(kv => kv.Key == propertyName);
        if (namedArg.Value.Value is ITypeSymbol typeSymbol)
        {
            return typeSymbol.ToDisplayString();
        }
        return null;
    }

    private static string? FindMethod(INamedTypeSymbol symbol, string[] possibleNames)
    {
        foreach (var name in possibleNames)
        {
            var method = symbol.GetMembers(name).OfType<IMethodSymbol>().FirstOrDefault();
            if (method != null)
                return name;
        }
        return null;
    }

    // Update main generation method to handle legacy methods
    private static void GenerateMediatorCode(
        SourceProductionContext context, 
        ImmutableArray<RequestInfoResult?> requestResults, 
        ImmutableArray<HandlerInfoResult?> handlerResults,
        ImmutableArray<LegacyMethodInfo?> legacyMethods)
    {
        // Report diagnostics (existing code)
        foreach (var requestResult in requestResults.Where(r => r != null))
        {
            foreach (var diagnostic in requestResult!.Diagnostics)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    diagnostic.Descriptor,
                    diagnostic.Location,
                    diagnostic.Args));
            }
        }

        foreach (var handlerResult in handlerResults.Where(h => h != null))
        {
            foreach (var diagnostic in handlerResult!.Diagnostics)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    diagnostic.Descriptor,
                    diagnostic.Location,
                    diagnostic.Args));
            }
        }

        // Extract valid items
        var validRequests = requestResults
            .Where(r => r?.RequestInfo != null)
            .Select(r => r!.RequestInfo!)
            .ToList();
        
        var validHandlers = handlerResults
            .Where(h => h?.HandlerInfo != null)
            .Select(h => h!.HandlerInfo!)
            .ToList();

        var validLegacyMethods = legacyMethods
            .Where(m => m != null)
            .Cast<LegacyMethodInfo>()
            .ToList();

        if (!validRequests.Any() && !validHandlers.Any() && !validLegacyMethods.Any()) return;

        try
        {
            // EXISTING: Generate CQRS-style request implementations
            foreach (var request in validRequests)
            {
                var requestSource = GenerateRequestImplementation(request);
                context.AddSource($"{request.Name}.Request.g.cs", SourceText.From(requestSource, Encoding.UTF8));
            }

            // EXISTING: Generate CQRS-style handler implementations
            foreach (var handler in validHandlers)
            {
                var handlerSource = GenerateHandlerImplementation(handler, validRequests);
                context.AddSource($"{handler.HandlerName}.Handler.g.cs", SourceText.From(handlerSource, Encoding.UTF8));
            }

            // NEW: Generate legacy method request classes and handlers
            foreach (var legacyMethod in validLegacyMethods)
            {
                // Generate request class
                var requestSource = GenerateLegacyRequestImplementation(legacyMethod);
                context.AddSource($"{legacyMethod.RequestName}.Request.g.cs", SourceText.From(requestSource, Encoding.UTF8));

                // Generate handler class  
                var handlerSource = GenerateLegacyHandlerImplementation(legacyMethod);
                context.AddSource($"{legacyMethod.HandlerName}.Handler.g.cs", SourceText.From(handlerSource, Encoding.UTF8));
            }

            // Generate extended mediator implementation
            var mediatorSource = GenerateExtendedMediatorImplementation(validRequests, validHandlers, validLegacyMethods);
            context.AddSource("GeneratedMediator.g.cs", SourceText.From(mediatorSource, Encoding.UTF8));

            // Generate extended DI extensions
            var diSource = GenerateExtendedDIExtensions(validHandlers, validLegacyMethods);
            context.AddSource("MediatorDIExtensions.g.cs", SourceText.From(diSource, Encoding.UTF8));
        }
        catch (System.Exception ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                MediatorDiagnostics.CodeGenerationError,
                Location.None,
                ex.Message));
        }
    }

    private static string GenerateRequestImplementation(RequestInfo request)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using CodeGenerator.Patterns.Mediator;");
        if (request.Type == RequestType.StreamQuery)
        {
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Runtime.CompilerServices;");
        }
        sb.AppendLine();

        if (!string.IsNullOrEmpty(request.Namespace))
        {
            sb.AppendLine($"namespace {request.Namespace};");
            sb.AppendLine();
        }

        // ALWAYS generate a CLASS (not record) that implements the mediator interface
        var interfaceName = request.Type switch
        {
            RequestType.Query => $"IQuery<{request.ResponseType ?? "object"}>",
            RequestType.Command when !string.IsNullOrEmpty(request.ResponseType) => $"ICommand<{request.ResponseType}>",
            RequestType.Command => "ICommand",
            RequestType.StreamQuery => $"IStreamQuery<{request.ResponseType ?? "object"}>",
            _ => "object"
        };

        sb.AppendLine($"// Generated from {request.ClassName} ({(request.IsRecord ? "record" : "class")})");
        sb.AppendLine($"public class {request.Name} : {interfaceName}");
        sb.AppendLine("{");

        // Generate properties regardless of whether source was class or record
        foreach (var property in request.Properties)
        {
            sb.AppendLine($"    public {property.Type} {property.Name} {{ get; set; }}{property.Initializer}");
        }

        sb.AppendLine("}");

        return sb.ToString();
    }
    
    // NEW: Generate request class for legacy method
    private static string GenerateLegacyRequestImplementation(LegacyMethodInfo method)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using CodeGenerator.Patterns.Mediator;");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(method.Namespace))
        {
            sb.AppendLine($"namespace {method.Namespace};");
            sb.AppendLine();
        }

        // Determine interface to implement
        var interfaceName = method.HasReturnType 
            ? $"IRequest<{method.ReturnType ?? "object"}>"
            : "IRequest";

        sb.AppendLine($"// Generated from method {method.ServiceClassName}.{method.MethodName}");
        sb.AppendLine($"public class {method.RequestName} : {interfaceName}");
        sb.AppendLine("{");

        // Generate properties from method parameters
        foreach (var parameter in method.Parameters)
        {
            var propertyName = char.ToUpper(parameter.Name[0]) + parameter.Name.Substring(1);
            sb.AppendLine($"    public {parameter.Type} {propertyName} {{ get; set; }}{parameter.Initializer}");
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string GenerateHandlerImplementation(HandlerInfo handler, List<RequestInfo> requests)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using CodeGenerator.Patterns.Mediator;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");
        if (handler.Type == HandlerType.StreamQuery)
        {
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Runtime.CompilerServices;");
        }
        sb.AppendLine();

        if (!string.IsNullOrEmpty(handler.Namespace))
        {
            sb.AppendLine($"namespace {handler.Namespace};");
            sb.AppendLine();
        }

        // For notification handlers, generate simple handlers that work directly with events
        if (handler.Type == HandlerType.Notification)
        {
            return GenerateNotificationHandler(handler);
        }

        // Find the corresponding request - use the NAME from the attribute, not the original class name
        var request = requests.FirstOrDefault(r => r.Name == handler.RequestType);
        if (request == null) return "";

        // Generate handler class that uses the NEW generated class name
        var interfaceName = handler.Type switch
        {
            HandlerType.Query => $"IQueryHandler<{request.Name}, {request.ResponseType ?? "object"}>",
            HandlerType.Command when !string.IsNullOrEmpty(request.ResponseType) => $"ICommandHandler<{request.Name}, {request.ResponseType}>",
            HandlerType.Command => $"ICommandHandler<{request.Name}>",
            HandlerType.StreamQuery => $"IStreamQueryHandler<{request.Name}, {request.ResponseType ?? "object"}>",
            _ => "object"
        };

        var serviceFullName = !string.IsNullOrEmpty(handler.Namespace) 
            ? $"{handler.Namespace}.{handler.ServiceClassName}"
            : handler.ServiceClassName;

        sb.AppendLine($"// Generated mediator handler for {handler.ServiceClassName}");
        sb.AppendLine($"public class {handler.HandlerName} : {interfaceName}");
        sb.AppendLine("{");
        sb.AppendLine($"    private readonly {serviceFullName} _service;");
        
        sb.AppendLine();
        sb.AppendLine($"    public {handler.HandlerName}({serviceFullName} service)");
        sb.AppendLine("    {");
        sb.AppendLine("        _service = service;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate Handle method
        var returnType = handler.Type switch
        {
            HandlerType.Query => $"Task<{request.ResponseType ?? "object"}>",
            HandlerType.Command when !string.IsNullOrEmpty(request.ResponseType) => $"Task<{request.ResponseType}>",
            HandlerType.StreamQuery => $"IAsyncEnumerable<{request.ResponseType ?? "object"}>",
            _ => "Task"
        };

        var parameterName = request.Type switch
        {
            RequestType.Query => "query",
            RequestType.Command => "command", 
            RequestType.StreamQuery => "query",
            _ => "request"
        };

        if (handler.Type == HandlerType.StreamQuery)
        {
            sb.AppendLine($"    public async {returnType} Handle({request.Name} {parameterName}, [EnumeratorCancellation] CancellationToken cancellationToken = default)");
        }
        else
        {
            sb.AppendLine($"    public async {returnType} Handle({request.Name} {parameterName}, CancellationToken cancellationToken = default)");
        }
        sb.AppendLine("    {");

        // Map from the new generated class/record to the original request class/record
        if (request.IsRecord)
        {
            // For records, use primary constructor
            var constructorArgs = new List<string>();
            foreach (var property in request.Properties)
            {
                constructorArgs.Add($"{parameterName}.{property.Name}");
            }
            sb.AppendLine($"        var originalRequest = new {request.ClassName}({string.Join(", ", constructorArgs)});");
        }
        else
        {
            // For classes, use object initializer
            sb.AppendLine($"        var originalRequest = new {request.ClassName}");
            sb.AppendLine("        {");
            foreach (var property in request.Properties)
            {
                sb.AppendLine($"            {property.Name} = {parameterName}.{property.Name},");
            }
            sb.AppendLine("        };");
        }
        sb.AppendLine();

        if (handler.Method != null)
        {
            if (handler.Type == HandlerType.StreamQuery)
            {
                sb.AppendLine($"        await foreach (var item in _service.{handler.Method}(originalRequest, cancellationToken))");
                
                sb.AppendLine("        {");
                sb.AppendLine("            yield return item;");
                sb.AppendLine("        }");
            }
            else if (handler.Type == HandlerType.Query || (handler.Type == HandlerType.Command && !string.IsNullOrEmpty(request.ResponseType)))
            {
                sb.AppendLine($"        return await _service.{handler.Method}(originalRequest, cancellationToken);");
            }
            else
            {
                sb.AppendLine($"        await _service.{handler.Method}(originalRequest, cancellationToken);");
            }
        }
        else
        {
            sb.AppendLine("        // TODO: Implement handler logic");
            sb.AppendLine("        throw new System.NotImplementedException();");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }
    
        // NEW: Generate handler class for legacy method
    private static string GenerateLegacyHandlerImplementation(LegacyMethodInfo method)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using CodeGenerator.Patterns.Mediator;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(method.Namespace))
        {
            sb.AppendLine($"namespace {method.Namespace};");
            sb.AppendLine();
        }

        // Determine handler interface to implement
        var handlerInterface = method.HasReturnType
            ? $"IRequestHandler<{method.RequestName}, {method.ReturnType ?? "object"}>"
            : $"IRequestHandler<{method.RequestName}>";

        var serviceFullName = !string.IsNullOrEmpty(method.Namespace) 
            ? $"{method.Namespace}.{method.ServiceClassName}"
            : method.ServiceClassName;

        sb.AppendLine($"// Generated handler for {method.ServiceClassName}.{method.MethodName}");
        sb.AppendLine($"public class {method.HandlerName} : {handlerInterface}");
        sb.AppendLine("{");
        sb.AppendLine($"    private readonly {serviceFullName} _service;");
        sb.AppendLine();
        sb.AppendLine($"    public {method.HandlerName}({serviceFullName} service)");
        sb.AppendLine("    {");
        sb.AppendLine("        _service = service;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate Handle method
        var returnType = method.HasReturnType 
            ? $"Task<{method.ReturnType ?? "object"}>"
            : "Task";

        sb.AppendLine($"    public async {returnType} Handle({method.RequestName} request, CancellationToken cancellationToken = default)");
        sb.AppendLine("    {");

        // Build method call arguments
        var args = new List<string>();
        foreach (var parameter in method.Parameters)
        {
            var propertyName = char.ToUpper(parameter.Name[0]) + parameter.Name.Substring(1);
            args.Add($"request.{propertyName}");
        }
        args.Add("cancellationToken");

        var methodCall = $"_service.{method.MethodName}({string.Join(", ", args)})";

        if (method.HasReturnType)
        {
            sb.AppendLine($"        return await {methodCall};");
        }
        else
        {
            sb.AppendLine($"        await {methodCall};");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string GenerateNotificationHandler(HandlerInfo handler)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(handler.Namespace))
        {
            sb.AppendLine($"namespace {handler.Namespace};");
            sb.AppendLine();
        }

        // Use the full type name with namespace for both event and service
        var eventTypeFullName = handler.RequestType ?? "object";
        var serviceFullName = !string.IsNullOrEmpty(handler.Namespace) 
            ? $"{handler.Namespace}.{handler.ServiceClassName}"
            : handler.ServiceClassName;
        
        sb.AppendLine($"// Generated notification handler for {handler.ServiceClassName}");
        sb.AppendLine($"public class {handler.HandlerName}");
        sb.AppendLine("{");
        sb.AppendLine($"    private readonly {serviceFullName} _service;");
        sb.AppendLine();
        sb.AppendLine($"    public {handler.HandlerName}({serviceFullName} service)");
        sb.AppendLine("    {");
        sb.AppendLine("        _service = service;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public async Task Handle({eventTypeFullName} eventObj, CancellationToken cancellationToken = default)");
        sb.AppendLine("    {");

        if (handler.Method != null)
        {
            sb.AppendLine($"        await _service.{handler.Method}(eventObj, cancellationToken);");
        }
        else
        {
            sb.AppendLine("        // TODO: Implement handler logic");
            sb.AppendLine("        throw new System.NotImplementedException();");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    // Update mediator generation to support both patterns
    private static string GenerateExtendedMediatorImplementation(
        List<RequestInfo> requests, 
        List<HandlerInfo> handlers, 
        List<LegacyMethodInfo> legacyMethods)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using CodeGenerator.Patterns.Mediator;");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();

        var allNamespaces = new List<string>();
        allNamespaces.AddRange(handlers.Where(h => !string.IsNullOrEmpty(h.Namespace)).Select(h => h.Namespace));
        allNamespaces.AddRange(legacyMethods.Where(m => !string.IsNullOrEmpty(m.Namespace)).Select(m => m.Namespace));
        
        var commonNamespace = allNamespaces
            .GroupBy(ns => ns)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key ?? "";

        if (!string.IsNullOrEmpty(commonNamespace))
        {
            sb.AppendLine($"namespace {commonNamespace};");
            sb.AppendLine();
        }

        sb.AppendLine("public sealed class GeneratedMediator : IMediator");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly IServiceProvider _serviceProvider;");
        sb.AppendLine();
        sb.AppendLine("    public GeneratedMediator(IServiceProvider serviceProvider)");
        sb.AppendLine("    {");
        sb.AppendLine("        _serviceProvider = serviceProvider;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // EXISTING CQRS methods
        GenerateQuerySendMethod(sb, requests, handlers);
        GenerateCommandSendMethod(sb, requests, handlers);
        GenerateCommandWithResponseSendMethod(sb, requests, handlers);
        GeneratePublishMethod(sb, requests, handlers);
        GenerateCreateStreamMethod(sb, requests, handlers);

        // NEW: Legacy MediatR-style methods
        GenerateLegacySendMethod(sb, legacyMethods);
        GenerateLegacySendWithResponseMethod(sb, legacyMethods);

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void GenerateQuerySendMethod(StringBuilder sb, List<RequestInfo> requests, List<HandlerInfo> handlers)
    {
        sb.AppendLine("    public async Task<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        return query switch");
        sb.AppendLine("        {");

        var queryRequests = requests.Where(r => r.Type == RequestType.Query).ToList();
        foreach (var request in queryRequests)
        {
            var handler = handlers.FirstOrDefault(h => h.RequestType == request.Name);
            if (handler != null)
            {
                var handlerFullName = !string.IsNullOrEmpty(handler.Namespace)
                    ? $"{handler.Namespace}.{handler.HandlerName}"
                    : handler.HandlerName;
                    
                sb.AppendLine($"            {request.Name} typedQuery => (TResponse)(object)await _serviceProvider.GetRequiredService<{handlerFullName}>().Handle(typedQuery, cancellationToken),");
            }
        }

        sb.AppendLine("            _ => throw new InvalidOperationException($\"No handler registered for query type {query.GetType().Name}\")");
        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GenerateCommandSendMethod(StringBuilder sb, List<RequestInfo> requests, List<HandlerInfo> handlers)
    {
        sb.AppendLine("    public async Task Send(ICommand command, CancellationToken cancellationToken = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        switch (command)");
        sb.AppendLine("        {");

        var commandRequests = requests.Where(r => r.Type == RequestType.Command && string.IsNullOrEmpty(r.ResponseType)).ToList();
        foreach (var request in commandRequests)
        {
            var handler = handlers.FirstOrDefault(h => h.RequestType == request.Name);
            if (handler != null)
            {
                var handlerFullName = !string.IsNullOrEmpty(handler.Namespace)
                    ? $"{handler.Namespace}.{handler.HandlerName}"
                    : handler.HandlerName;
                    
                sb.AppendLine($"            case {request.Name} typedCommand:");
                sb.AppendLine($"                await _serviceProvider.GetRequiredService<{handlerFullName}>().Handle(typedCommand, cancellationToken);");
                sb.AppendLine("                break;");
            }
        }

        sb.AppendLine("            default:");
        sb.AppendLine("                throw new InvalidOperationException($\"No handler registered for command type {command.GetType().Name}\");");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GenerateCommandWithResponseSendMethod(StringBuilder sb, List<RequestInfo> requests, List<HandlerInfo> handlers)
    {
        sb.AppendLine("    public async Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        return command switch");
        sb.AppendLine("        {");

        var commandRequests = requests.Where(r => r.Type == RequestType.Command && !string.IsNullOrEmpty(r.ResponseType)).ToList();
        foreach (var request in commandRequests)
        {
            var handler = handlers.FirstOrDefault(h => h.RequestType == request.Name);
            if (handler != null)
            {
                var handlerFullName = !string.IsNullOrEmpty(handler.Namespace)
                    ? $"{handler.Namespace}.{handler.HandlerName}"
                    : handler.HandlerName;
                    
                sb.AppendLine($"            {request.Name} typedCommand => (TResponse)(object)await _serviceProvider.GetRequiredService<{handlerFullName}>().Handle(typedCommand, cancellationToken),");
            }
        }

        sb.AppendLine("            _ => throw new InvalidOperationException($\"No handler registered for command type {command.GetType().Name}\")");
        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine();
    }
    
        // NEW: Generate legacy Send method for IRequest
    private static void GenerateLegacySendMethod(StringBuilder sb, List<LegacyMethodInfo> legacyMethods)
    {
        sb.AppendLine("    // Legacy MediatR-style Send method for IRequest");
        sb.AppendLine("    public async Task Send(IRequest request, CancellationToken cancellationToken = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        switch (request)");
        sb.AppendLine("        {");

        var voidMethods = legacyMethods.Where(m => !m.HasReturnType).ToList();
        foreach (var method in voidMethods)
        {
            var handlerFullName = !string.IsNullOrEmpty(method.Namespace)
                ? $"{method.Namespace}.{method.HandlerName}"
                : method.HandlerName;
                
            sb.AppendLine($"            case {method.RequestName} typedRequest:");
            sb.AppendLine($"                await _serviceProvider.GetRequiredService<{handlerFullName}>().Handle(typedRequest, cancellationToken);");
            sb.AppendLine("                break;");
        }

        sb.AppendLine("            default:");
        sb.AppendLine("                throw new InvalidOperationException($\"No handler registered for request type {request.GetType().Name}\");");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    // NEW: Generate legacy Send method for IRequest<T>
    private static void GenerateLegacySendWithResponseMethod(StringBuilder sb, List<LegacyMethodInfo> legacyMethods)
    {
        sb.AppendLine("    // Legacy MediatR-style Send method for IRequest<T>");
        sb.AppendLine("    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        return request switch");
        sb.AppendLine("        {");

        var responseMethods = legacyMethods.Where(m => m.HasReturnType).ToList();
        foreach (var method in responseMethods)
        {
            var handlerFullName = !string.IsNullOrEmpty(method.Namespace)
                ? $"{method.Namespace}.{method.HandlerName}"
                : method.HandlerName;
                
            sb.AppendLine($"            {method.RequestName} typedRequest => (TResponse)(object)await _serviceProvider.GetRequiredService<{handlerFullName}>().Handle(typedRequest, cancellationToken),");
        }

        sb.AppendLine("            _ => throw new InvalidOperationException($\"No handler registered for request type {request.GetType().Name}\")");
        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GeneratePublishMethod(StringBuilder sb, List<RequestInfo> requests, List<HandlerInfo> handlers)
    {
        sb.AppendLine("    public async Task Publish<TEvent>(TEvent eventObj, CancellationToken cancellationToken = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        var eventType = typeof(TEvent);");
        sb.AppendLine("        var tasks = new List<Task>();");
        sb.AppendLine();

        var notificationHandlers = handlers.Where(h => h.Type == HandlerType.Notification).ToList();
        
        foreach (var handler in notificationHandlers)
        {
            var eventTypeFullName = handler.RequestType ?? "object";
            var eventTypeName = eventTypeFullName.Split('.').Last();
            
            sb.AppendLine($"        // Handler: {handler.HandlerName} for {eventTypeFullName}");
            sb.AppendLine($"        if (typeof({eventTypeFullName}).IsAssignableFrom(eventType))");
            sb.AppendLine("        {");
            sb.AppendLine($"            tasks.Add(_serviceProvider.GetRequiredService<{handler.Namespace}.{handler.HandlerName}>().Handle(({eventTypeFullName})(object)eventObj!, cancellationToken));");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        sb.AppendLine("        if (tasks.Count > 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            await Task.WhenAll(tasks);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
    }

    private static void GenerateCreateStreamMethod(StringBuilder sb, List<RequestInfo> requests, List<HandlerInfo> handlers)
    {
        sb.AppendLine();
        sb.AppendLine("    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamQuery<TResponse> query, CancellationToken cancellationToken = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        return query switch");
        sb.AppendLine("        {");

        var streamRequests = requests.Where(r => r.Type == RequestType.StreamQuery).ToList();
        foreach (var request in streamRequests)
        {
            var handler = handlers.FirstOrDefault(h => h.RequestType == request.Name && h.Type == HandlerType.StreamQuery);
            if (handler != null)
            {
                var handlerFullName = !string.IsNullOrEmpty(handler.Namespace)
                    ? $"{handler.Namespace}.{handler.HandlerName}"
                    : handler.HandlerName;
                    
                sb.AppendLine($"            {request.Name} typedQuery => (IAsyncEnumerable<TResponse>)_serviceProvider.GetRequiredService<{handlerFullName}>().Handle(typedQuery, cancellationToken),");
            }
        }

        sb.AppendLine("            _ => throw new InvalidOperationException($\"No handler registered for stream query type {query.GetType().Name}\")");
        sb.AppendLine("        };");
        sb.AppendLine("    }");
    }

    // Update DI registration to include legacy handlers
    private static string GenerateExtendedDIExtensions(List<HandlerInfo> handlers, List<LegacyMethodInfo> legacyMethods)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using CodeGenerator.Patterns.Mediator;");
        sb.AppendLine();

        var allNamespaces = new List<string>();
        allNamespaces.AddRange(handlers.Where(h => !string.IsNullOrEmpty(h.Namespace)).Select(h => h.Namespace));
        allNamespaces.AddRange(legacyMethods.Where(m => !string.IsNullOrEmpty(m.Namespace)).Select(m => m.Namespace));
        
        var commonNamespace = allNamespaces
            .GroupBy(ns => ns)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key ?? "";

        if (!string.IsNullOrEmpty(commonNamespace))
        {
            sb.AppendLine($"namespace {commonNamespace};");
            sb.AppendLine();
        }

        sb.AppendLine("public static class MediatorServiceCollectionExtensions");
        sb.AppendLine("{");
        sb.AppendLine("    public static IServiceCollection AddDeclarativeMediator(this IServiceCollection services)");
        sb.AppendLine("    {");

        // EXISTING: Register CQRS-style handlers and services
        foreach (var handler in handlers)
        {
            var serviceFullName = !string.IsNullOrEmpty(handler.Namespace) 
                ? $"{handler.Namespace}.{handler.ServiceClassName}"
                : handler.ServiceClassName;
                
            var handlerFullName = !string.IsNullOrEmpty(handler.Namespace)
                ? $"{handler.Namespace}.{handler.HandlerName}"
                : handler.HandlerName;
                
            sb.AppendLine($"        services.AddScoped<{serviceFullName}>();");
            sb.AppendLine($"        services.AddScoped<{handlerFullName}>();");
        }

        // NEW: Register legacy method handlers and services
        var uniqueServices = legacyMethods
            .GroupBy(m => $"{m.Namespace}.{m.ServiceClassName}")
            .Select(g => g.First())
            .ToList();

        foreach (var service in uniqueServices)
        {
            var serviceFullName = !string.IsNullOrEmpty(service.Namespace) 
                ? $"{service.Namespace}.{service.ServiceClassName}"
                : service.ServiceClassName;
                
            sb.AppendLine($"        services.AddScoped<{serviceFullName}>();");
        }

        foreach (var method in legacyMethods)
        {
            var handlerFullName = !string.IsNullOrEmpty(method.Namespace)
                ? $"{method.Namespace}.{method.HandlerName}"
                : method.HandlerName;
                
            sb.AppendLine($"        services.AddScoped<{handlerFullName}>();");
        }

        sb.AppendLine();
        sb.AppendLine("        services.AddSingleton<IMediator, GeneratedMediator>();");
        sb.AppendLine("        return services;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }
}