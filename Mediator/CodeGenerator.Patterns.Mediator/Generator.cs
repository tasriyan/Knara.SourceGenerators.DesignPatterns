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

namespace CodeGenerator.Patterns.Mediator
{
    // Request pattern attributes
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

    // Handler pattern attributes
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

    // Base interfaces for mediator pattern
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

    public interface IMediator
    {
        Task<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);
        Task Send(ICommand command, CancellationToken cancellationToken = default);
        Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);
        Task Publish<TEvent>(TEvent eventObj, CancellationToken cancellationToken = default);
        IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamQuery<TResponse> query, CancellationToken cancellationToken = default);
    }
}";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Add the mediator attribute interfaces
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource("MediatorAttributes.g.cs", SourceText.From(MediatorAttributes, Encoding.UTF8));
        });

        // Get request classes (Query, Command, StreamQuery)
        var requestClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsRequestClass(s),
                transform: static (ctx, _) => GetRequestInfo(ctx))
            .Where(static m => m is not null);

        // Get handler classes (QueryHandler, CommandHandler, NotificationHandler, StreamQueryHandler)
        var handlerClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsHandlerClass(s),
                transform: static (ctx, _) => GetHandlerInfo(ctx))
            .Where(static m => m is not null);

        // Combine and generate
        var combined = requestClasses.Collect().Combine(handlerClasses.Collect());
        context.RegisterSourceOutput(combined, static (spc, source) => GenerateMediatorCode(spc, source.Left, source.Right));
    }

    private static bool IsRequestClass(SyntaxNode node)
    {
        return (node is ClassDeclarationSyntax cls && cls.AttributeLists.Count > 0) ||
               (node is RecordDeclarationSyntax rec && rec.AttributeLists.Count > 0);
    }

    private static bool IsHandlerClass(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax cls && cls.AttributeLists.Count > 0;
    }

    private static RequestInfo? GetRequestInfo(GeneratorSyntaxContext context)
    {
        var model = context.SemanticModel;
        INamedTypeSymbol? symbol = null;
        bool isRecord = false;

        // Handle both class and record declarations
        if (context.Node is ClassDeclarationSyntax classDecl)
        {
            symbol = model.GetDeclaredSymbol(classDecl);
            isRecord = false;
        }
        else if (context.Node is RecordDeclarationSyntax recordDecl)
        {
            symbol = model.GetDeclaredSymbol(recordDecl);
            isRecord = true;
        }

        if (symbol == null) return null;

        // Extract properties from the class/record
        var properties = ExtractProperties(symbol, isRecord);

        var queryAttr = GetAttribute(symbol, "QueryAttribute");
        if (queryAttr != null)
        {
            var name = GetAttributeStringValue(queryAttr, "Name");
            var responseType = GetAttributeTypeValue(queryAttr, "ResponseType");
            return new RequestInfo(symbol.Name, name, RequestType.Query, responseType, symbol.ContainingNamespace?.ToDisplayString() ?? "", properties, isRecord);
        }

        var commandAttr = GetAttribute(symbol, "CommandAttribute");
        if (commandAttr != null)
        {
            var name = GetAttributeStringValue(commandAttr, "Name");
            var responseType = GetAttributeTypeValue(commandAttr, "ResponseType");
            return new RequestInfo(symbol.Name, name, RequestType.Command, responseType, symbol.ContainingNamespace?.ToDisplayString() ?? "", properties, isRecord);
        }

        var streamQueryAttr = GetAttribute(symbol, "StreamQueryAttribute");
        if (streamQueryAttr != null)
        {
            var name = GetAttributeStringValue(streamQueryAttr, "Name");
            var responseType = GetAttributeTypeValue(streamQueryAttr, "ResponseType");
            return new RequestInfo(symbol.Name, name, RequestType.StreamQuery, responseType, symbol.ContainingNamespace?.ToDisplayString() ?? "", properties, isRecord);
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

    private static HandlerInfo? GetHandlerInfo(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var model = context.SemanticModel;
        var symbol = model.GetDeclaredSymbol(classDecl);

        if (symbol == null) return null;

        var queryHandlerAttr = GetAttribute(symbol, "QueryHandlerAttribute");
        if (queryHandlerAttr != null)
        {
            var name = GetAttributeStringValue(queryHandlerAttr, "Name");
            var requestType = GetAttributeTypeValue(queryHandlerAttr, "RequestType");
            var method = FindMethod(symbol, new[] { "GetAsync", "QueryAsync", "ExecuteAsync" });
            return new HandlerInfo(symbol.Name, name, HandlerType.Query, requestType, null, method, symbol.ContainingNamespace?.ToDisplayString() ?? "");
        }

        var commandHandlerAttr = GetAttribute(symbol, "CommandHandlerAttribute");
        if (commandHandlerAttr != null)
        {
            var name = GetAttributeStringValue(commandHandlerAttr, "Name");
            var requestType = GetAttributeTypeValue(commandHandlerAttr, "RequestType");
            var publisherType = GetAttributeTypeValue(commandHandlerAttr, "PublisherType");
            var method = FindMethod(symbol, new[] { "CreateAsync", "UpdateAsync", "DeleteAsync", "ExecuteAsync", "ProcessAsync" });
            return new HandlerInfo(symbol.Name, name, HandlerType.Command, requestType, publisherType, method, symbol.ContainingNamespace?.ToDisplayString() ?? "");
        }

        var notificationHandlerAttr = GetAttribute(symbol, "NotificationHandlerAttribute");
        if (notificationHandlerAttr != null)
        {
            var name = GetAttributeStringValue(notificationHandlerAttr, "Name");
            var eventType = GetAttributeTypeValue(notificationHandlerAttr, "EventType");
            var method = FindMethod(symbol, new[] { "ProcessAsync", "HandleAsync", "ExecuteAsync" });
            return new HandlerInfo(symbol.Name, name, HandlerType.Notification, eventType, null, method, symbol.ContainingNamespace?.ToDisplayString() ?? "");
        }
        
        var streamQueryHandlerAttr = GetAttribute(symbol, "StreamQueryHandlerAttribute");
        if (streamQueryHandlerAttr != null)
        {
            var name = GetAttributeStringValue(streamQueryHandlerAttr, "Name");
            var requestType = GetAttributeTypeValue(streamQueryHandlerAttr, "RequestType");
            var method = FindMethod(symbol, new[] { "GetAsync", "HandleAsync", "ExecuteAsync" });
            return new HandlerInfo(symbol.Name, 
                name, 
                HandlerType.StreamQuery, 
                requestType, 
                null, 
                method, 
                symbol.ContainingNamespace?.ToDisplayString() ?? "");
        }

        return null;
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

    private static string? GetAttributeTypeValueAsString(AttributeData attribute, string propertyName)
    {
        var namedArg = attribute.NamedArguments.FirstOrDefault(kv => kv.Key == propertyName);
        if (namedArg.Value.Value is ITypeSymbol typeSymbol)
        {
            // Return just the type name, not the full namespace
            return typeSymbol.Name;
        }
        return null;
    }

    private static int GetAttributeIntValue(AttributeData attribute, string propertyName, int defaultValue)
    {
        var namedArg = attribute.NamedArguments.FirstOrDefault(kv => kv.Key == propertyName);
        if (namedArg.Value.Value is int intValue)
        {
            return intValue;
        }
        return defaultValue;
    }

    private static bool GetAttributeBoolValue(AttributeData attribute, string propertyName, bool defaultValue)
    {
        var namedArg = attribute.NamedArguments.FirstOrDefault(kv => kv.Key == propertyName);
        if (namedArg.Value.Value is bool boolValue)
        {
            return boolValue;
        }
        return defaultValue;
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

    private static void GenerateMediatorCode(SourceProductionContext context, ImmutableArray<RequestInfo?> requests, ImmutableArray<HandlerInfo?> handlers)
    {
        var validRequests = requests.Where(r => r != null).Cast<RequestInfo>().ToList();
        var validHandlers = handlers.Where(h => h != null).Cast<HandlerInfo>().ToList();

        if (!validRequests.Any() && !validHandlers.Any()) return;

        // Generate request types that implement mediator interfaces
        foreach (var request in validRequests)
        {
            var requestSource = GenerateRequestImplementation(request);
            context.AddSource($"{request.Name}.Request.g.cs", SourceText.From(requestSource, Encoding.UTF8));
        }

        // Generate handler implementations
        foreach (var handler in validHandlers)
        {
            var handlerSource = GenerateHandlerImplementation(handler, validRequests);
            context.AddSource($"{handler.HandlerName}.Handler.g.cs", SourceText.From(handlerSource, Encoding.UTF8));
        }

        // Generate mediator implementation
        var mediatorSource = GenerateMediatorImplementation(validRequests, validHandlers);
        context.AddSource("GeneratedMediator.g.cs", SourceText.From(mediatorSource, Encoding.UTF8));

        // Generate DI extensions
        var diSource = GenerateDIExtensions(validHandlers);
        context.AddSource("MediatorDIExtensions.g.cs", SourceText.From(diSource, Encoding.UTF8));
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

    private static string GenerateMediatorImplementation(List<RequestInfo> requests, List<HandlerInfo> handlers)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using CodeGenerator.Patterns.Mediator;");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using System.Collections.Generic;"); // Added for streaming support
        sb.AppendLine();

        var commonNamespace = GetMostCommonNamespace(handlers);
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

        // Generate Send method for queries
        GenerateQuerySendMethod(sb, requests, handlers);

        // Generate Send method for commands (void)
        GenerateCommandSendMethod(sb, requests, handlers);

        // Generate Send method for commands with response
        GenerateCommandWithResponseSendMethod(sb, requests, handlers);

        // Generate Publish method for notifications
        GeneratePublishMethod(sb, requests, handlers);

        // Generate CreateStream method for stream queries
        GenerateCreateStreamMethod(sb, requests, handlers);

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

    private static string GenerateDIExtensions(List<HandlerInfo> handlers)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using CodeGenerator.Patterns.Mediator;");
        sb.AppendLine();

        var commonNamespace = GetMostCommonNamespace(handlers);
        if (!string.IsNullOrEmpty(commonNamespace))
        {
            sb.AppendLine($"namespace {commonNamespace};");
            sb.AppendLine();
        }

        sb.AppendLine("public static class MediatorServiceCollectionExtensions");
        sb.AppendLine("{");
        sb.AppendLine("    public static IServiceCollection AddDeclarativeMediator(this IServiceCollection services)");
        sb.AppendLine("    {");

        // Register all handlers and their services
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

        sb.AppendLine();
        sb.AppendLine("        services.AddSingleton<IMediator, GeneratedMediator>();");
        sb.AppendLine("        return services;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string GetMostCommonNamespace(List<HandlerInfo> handlers)
    {
        return handlers
            .Where(h => !string.IsNullOrEmpty(h.Namespace))
            .GroupBy(h => h.Namespace)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key ?? "";
    }
}