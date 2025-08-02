// using System.Collections.Generic;
// using System.Linq;
// using System.Text;
// using Microsoft.CodeAnalysis;
// using Microsoft.CodeAnalysis.CSharp.Syntax;
// using Microsoft.CodeAnalysis.Text;
//
// namespace SourceGenerators.DesignPatterns.Decorator.Old;
//
// [Generator]
// public class DecoratorPatternGenerator : IIncrementalGenerator
// {
//     private const string AttributesSource = """
//         using System;
//
//         namespace Demo.Generator;
//
//         [AttributeUsage(AttributeTargets.Interface)]
//         public class GenerateDecoratorsAttribute : Attribute
//         {
//             public string? BaseName { get; set; }
//             public bool GenerateFactory { get; set; } = true;
//             public bool GenerateAsyncSupport { get; set; } = true;
//             public DecoratorAccessibility Accessibility { get; set; } = DecoratorAccessibility.Public;
//         }
//
//         [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
//         public class DecoratorAttribute : Attribute
//         {
//             public DecoratorType Type { get; set; }
//             public string? Name { get; set; }
//             public string? LoggerProperty { get; set; }
//             public string? CacheKeyFormat { get; set; }
//             public int CacheExpirationMinutes { get; set; } = 60;
//             public string? ValidationMethod { get; set; }
//             public int RetryAttempts { get; set; } = 3;
//             public string? MetricName { get; set; }
//         }
//
//         public enum DecoratorAccessibility
//         {
//             Public,
//             Internal,
//             Private
//         }
//
//         public enum DecoratorType
//         {
//             Logging,
//             Caching,
//             Validation,
//             Retry,
//             Performance,
//             Authorization,
//             CircuitBreaker
//         }
//         """;
//
//     public void Initialize(IncrementalGeneratorInitializationContext context)
//     {
//         // Generate the attributes as source code
//         context.RegisterPostInitializationOutput(ctx => ctx
//             .AddSource("DecoratorAttributes.g.cs",
//                 SourceText.From(AttributesSource, Encoding.UTF8)));
//
//         // Find all interfaces with [GenerateDecorators] attribute
//         var decoratorCandidates = context.SyntaxProvider
//             .CreateSyntaxProvider(
//                 predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
//                 transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
//             .Where(static m => m is not null);
//         
//         // Generate decorators for each candidate
//         context.RegisterSourceOutput(decoratorCandidates, static (spc, source) => Execute(source!, spc));
//     }
//
//     static bool IsSyntaxTargetForGeneration(SyntaxNode node)
//     {
//         return node is InterfaceDeclarationSyntax;
//     }
//
//     static InterfaceToDecorate? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
//     {
//         var interfaceDeclaration = (InterfaceDeclarationSyntax)context.Node;
//
//         // Check if the interface has the GenerateDecorators attribute
//         foreach (var attributeList in interfaceDeclaration.AttributeLists)
//         {
//             foreach (var attribute in attributeList.Attributes)
//             {
//                 var symbolInfo = context.SemanticModel.GetSymbolInfo(attribute);
//                 if (symbolInfo.Symbol is IMethodSymbol attributeSymbol)
//                 {
//                     var attributeType = attributeSymbol.ContainingType;
//                     if (attributeType.Name == "GenerateDecoratorsAttribute" &&
//                         attributeType.ContainingNamespace?.ToDisplayString() == "Demo.Generator")
//                     {
//                         return CreateInterfaceToDecorate(context, interfaceDeclaration);
//                     }
//                 }
//             }
//         }
//
//         return null;
//     }
//
//     static InterfaceToDecorate CreateInterfaceToDecorate(GeneratorSyntaxContext context, InterfaceDeclarationSyntax interfaceDeclaration)
//     {
//         var interfaceSymbol = context.SemanticModel.GetDeclaredSymbol(interfaceDeclaration) as INamedTypeSymbol;
//         if (interfaceSymbol == null) return null;
//
//         var methods = new List<MethodToDecorate>();
//
//         foreach (var member in interfaceSymbol.GetMembers().OfType<IMethodSymbol>())
//         {
//             if (member.MethodKind != MethodKind.Ordinary) continue;
//
//             var method = CreateMethodToDecorate(member);
//             if (method != null)
//             {
//                 methods.Add(method);
//             }
//         }
//
//         var decoratorAttribute = GetDecoratorAttribute(interfaceSymbol);
//
//         return new InterfaceToDecorate
//         {
//             Name = interfaceSymbol.Name,
//             Namespace = interfaceSymbol.ContainingNamespace.ToDisplayString(),
//             Methods = methods,
//             BaseName = decoratorAttribute?.BaseName ?? interfaceSymbol.Name.TrimStart('I'),
//             GenerateFactory = decoratorAttribute?.GenerateFactory ?? true,
//             GenerateAsyncSupport = decoratorAttribute?.GenerateAsyncSupport ?? true,
//             Accessibility = decoratorAttribute?.Accessibility ?? DecoratorAccessibility.Public
//         };
//     }
//
//     static MethodToDecorate CreateMethodToDecorate(IMethodSymbol methodSymbol)
//     {
//         var decorators = GetMethodDecorators(methodSymbol);
//         var isAsync = methodSymbol.ReturnType.Name == "Task" || 
//                      (methodSymbol.ReturnType is INamedTypeSymbol namedType && 
//                       namedType.IsGenericType && namedType.ConstructedFrom.Name == "Task");
//
//         return new MethodToDecorate
//         {
//             Name = methodSymbol.Name,
//             ReturnType = methodSymbol.ReturnType.ToDisplayString(),
//             Parameters = methodSymbol.Parameters.Select(p => new ParameterInfo
//             {
//                 Name = p.Name,
//                 Type = p.Type.ToDisplayString(),
//                 HasDefaultValue = p.HasExplicitDefaultValue,
//                 DefaultValue = p.ExplicitDefaultValue?.ToString()
//             }).ToList(),
//             IsAsync = isAsync,
//             Decorators = decorators
//         };
//     }
//
//     static void Execute(InterfaceToDecorate interfaceToDecorate, SourceProductionContext context)
//     {
//         var sourceBuilder = new StringBuilder();
//
//         // Generate the decorator classes
//         sourceBuilder.AppendLine("// <auto-generated />");
//         sourceBuilder.AppendLine("using System;");
//         sourceBuilder.AppendLine("using System.Threading.Tasks;");
//         sourceBuilder.AppendLine("using System.Diagnostics;");
//         sourceBuilder.AppendLine("using Microsoft.Extensions.Logging;");
//         sourceBuilder.AppendLine("using Microsoft.Extensions.Caching.Memory;");
//         sourceBuilder.AppendLine();
//
//         if (!string.IsNullOrEmpty(interfaceToDecorate.Namespace))
//         {
//             sourceBuilder.AppendLine($"namespace {interfaceToDecorate.Namespace}");
//             sourceBuilder.AppendLine("{");
//         }
//
//         // Generate base decorator
//         GenerateBaseDecorator(sourceBuilder, interfaceToDecorate);
//
//         // Generate specific decorators
//         GenerateLoggingDecorator(sourceBuilder, interfaceToDecorate);
//         GenerateCachingDecorator(sourceBuilder, interfaceToDecorate);
//         GenerateValidationDecorator(sourceBuilder, interfaceToDecorate);
//         GenerateRetryDecorator(sourceBuilder, interfaceToDecorate);
//         GeneratePerformanceDecorator(sourceBuilder, interfaceToDecorate);
//
//         // Generate factory
//         if (interfaceToDecorate.GenerateFactory)
//         {
//             GenerateDecoratorFactory(sourceBuilder, interfaceToDecorate);
//         }
//
//         if (!string.IsNullOrEmpty(interfaceToDecorate.Namespace))
//         {
//             sourceBuilder.AppendLine("}");
//         }
//
//         context.AddSource($"{interfaceToDecorate.Name}Decorators.g.cs", 
//             SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
//     }
//
//     static void GenerateBaseDecorator(StringBuilder sb, InterfaceToDecorate iface)
//     {
//         var accessibility = iface.Accessibility.ToString().ToLower();
//         
//         sb.AppendLine($"    {accessibility} abstract class {iface.BaseName}DecoratorBase : {iface.Name}");
//         sb.AppendLine("    {");
//         sb.AppendLine($"        protected readonly {iface.Name} _inner;");
//         sb.AppendLine();
//         sb.AppendLine($"        protected {iface.BaseName}DecoratorBase({iface.Name} inner)");
//         sb.AppendLine("        {");
//         sb.AppendLine("            _inner = inner ?? throw new ArgumentNullException(nameof(inner));");
//         sb.AppendLine("        }");
//         sb.AppendLine();
//
//         foreach (var method in iface.Methods)
//         {
//             GenerateBaseDecoratorMethod(sb, method);
//         }
//
//         sb.AppendLine("    }");
//         sb.AppendLine();
//     }
//
//     static void GenerateBaseDecoratorMethod(StringBuilder sb, MethodToDecorate method)
//     {
//         var parameters = string.Join(", ", method.Parameters.Select(p => $"{p.Type} {p.Name}"));
//         var arguments = string.Join(", ", method.Parameters.Select(p => p.Name));
//
//         sb.AppendLine($"        public virtual {method.ReturnType} {method.Name}({parameters})");
//         sb.AppendLine("        {");
//         
//         if (method.ReturnType == "void")
//         {
//             sb.AppendLine($"            _inner.{method.Name}({arguments});");
//         }
//         else
//         {
//             sb.AppendLine($"            return _inner.{method.Name}({arguments});");
//         }
//         
//         sb.AppendLine("        }");
//         sb.AppendLine();
//     }
//
//     static void GenerateLoggingDecorator(StringBuilder sb, InterfaceToDecorate iface)
//     {
//         var accessibility = iface.Accessibility.ToString().ToLower();
//         
//         sb.AppendLine($"    {accessibility} class {iface.BaseName}LoggingDecorator : {iface.BaseName}DecoratorBase");
//         sb.AppendLine("    {");
//         sb.AppendLine("        private readonly ILogger _logger;");
//         sb.AppendLine();
//         sb.AppendLine($"        public {iface.BaseName}LoggingDecorator({iface.Name} inner, ILogger logger)");
//         sb.AppendLine("            : base(inner)");
//         sb.AppendLine("        {");
//         sb.AppendLine("            _logger = logger ?? throw new ArgumentNullException(nameof(logger));");
//         sb.AppendLine("        }");
//         sb.AppendLine();
//
//         foreach (var method in iface.Methods.Where(m => m.Decorators.Any(d => d.Type == DecoratorType.Logging)))
//         {
//             GenerateLoggingMethod(sb, method, iface.BaseName);
//         }
//
//         sb.AppendLine("    }");
//         sb.AppendLine();
//     }
//
//     static void GenerateLoggingMethod(StringBuilder sb, MethodToDecorate method, string baseName)
//     {
//         var parameters = string.Join(", ", method.Parameters.Select(p => $"{p.Type} {p.Name}"));
//         var arguments = string.Join(", ", method.Parameters.Select(p => p.Name));
//         var logArgs = string.Join(", ", method.Parameters.Select(p => $"{{@{p.Name}}}"));
//
//         if (method.IsAsync)
//         {
//             sb.AppendLine($"        public override async {method.ReturnType} {method.Name}({parameters})");
//             sb.AppendLine("        {");
//             sb.AppendLine($"            _logger.LogInformation(\"Calling {method.Name}({logArgs})\", {arguments});");
//             sb.AppendLine("            try");
//             sb.AppendLine("            {");
//             
//             if (method.ReturnType == "Task")
//             {
//                 sb.AppendLine($"                await _inner.{method.Name}({arguments});");
//                 sb.AppendLine($"                _logger.LogInformation(\"{method.Name} completed successfully\");");
//             }
//             else
//             {
//                 sb.AppendLine($"                var result = await _inner.{method.Name}({arguments});");
//                 sb.AppendLine($"                _logger.LogInformation(\"{method.Name} completed successfully with result: {{@Result}}\", result);");
//                 sb.AppendLine("                return result;");
//             }
//             
//             sb.AppendLine("            }");
//             sb.AppendLine("            catch (Exception ex)");
//             sb.AppendLine("            {");
//             sb.AppendLine($"                _logger.LogError(ex, \"Error in {method.Name}\");");
//             sb.AppendLine("                throw;");
//             sb.AppendLine("            }");
//         }
//         else
//         {
//             sb.AppendLine($"        public override {method.ReturnType} {method.Name}({parameters})");
//             sb.AppendLine("        {");
//             sb.AppendLine($"            _logger.LogInformation(\"Calling {method.Name}({logArgs})\", {arguments});");
//             sb.AppendLine("            try");
//             sb.AppendLine("            {");
//             
//             if (method.ReturnType == "void")
//             {
//                 sb.AppendLine($"                _inner.{method.Name}({arguments});");
//                 sb.AppendLine($"                _logger.LogInformation(\"{method.Name} completed successfully\");");
//             }
//             else
//             {
//                 sb.AppendLine($"                var result = _inner.{method.Name}({arguments});");
//                 sb.AppendLine($"                _logger.LogInformation(\"{method.Name} completed successfully with result: {{@Result}}\", result);");
//                 sb.AppendLine("                return result;");
//             }
//             
//             sb.AppendLine("            }");
//             sb.AppendLine("            catch (Exception ex)");
//             sb.AppendLine("            {");
//             sb.AppendLine($"                _logger.LogError(ex, \"Error in {method.Name}\");");
//             sb.AppendLine("                throw;");
//             sb.AppendLine("            }");
//         }
//         
//         sb.AppendLine("        }");
//         sb.AppendLine();
//     }
//
//     static void GenerateCachingDecorator(StringBuilder sb, InterfaceToDecorate iface)
//     {
//         var accessibility = iface.Accessibility.ToString().ToLower();
//         
//         sb.AppendLine($"    {accessibility} class {iface.BaseName}CachingDecorator : {iface.BaseName}DecoratorBase");
//         sb.AppendLine("    {");
//         sb.AppendLine("        private readonly IMemoryCache _cache;");
//         sb.AppendLine();
//         sb.AppendLine($"        public {iface.BaseName}CachingDecorator({iface.Name} inner, IMemoryCache cache)");
//         sb.AppendLine("            : base(inner)");
//         sb.AppendLine("        {");
//         sb.AppendLine("            _cache = cache ?? throw new ArgumentNullException(nameof(cache));");
//         sb.AppendLine("        }");
//         sb.AppendLine();
//
//         foreach (var method in iface.Methods.Where(m => m.Decorators.Any(d => d.Type == DecoratorType.Caching)))
//         {
//             GenerateCachingMethod(sb, method);
//         }
//
//         sb.AppendLine("    }");
//         sb.AppendLine();
//     }
//
//     static void GenerateCachingMethod(StringBuilder sb, MethodToDecorate method)
//     {
//         if (method.ReturnType == "void" || method.ReturnType == "Task") return; // Can't cache void methods
//
//         var parameters = string.Join(", ", method.Parameters.Select(p => $"{p.Type} {p.Name}"));
//         var arguments = string.Join(", ", method.Parameters.Select(p => p.Name));
//         var cacheKey = $"\"{method.Name}_\" + string.Join(\"_\", new object[] {{ {arguments} }})";
//         var decorator = method.Decorators.First(d => d.Type == DecoratorType.Caching);
//
//         if (method.IsAsync)
//         {
//             sb.AppendLine($"        public override async {method.ReturnType} {method.Name}({parameters})");
//             sb.AppendLine("        {");
//             sb.AppendLine($"            var cacheKey = {cacheKey};");
//             sb.AppendLine("            if (_cache.TryGetValue(cacheKey, out var cachedResult))");
//             sb.AppendLine("            {");
//             sb.AppendLine($"                return ({method.ReturnType.Replace("Task<", "").TrimEnd('>')})cachedResult;");
//             sb.AppendLine("            }");
//             sb.AppendLine();
//             sb.AppendLine($"            var result = await _inner.{method.Name}({arguments});");
//             sb.AppendLine($"            _cache.Set(cacheKey, result, TimeSpan.FromMinutes({decorator.CacheExpirationMinutes}));");
//             sb.AppendLine("            return result;");
//         }
//         else
//         {
//             sb.AppendLine($"        public override {method.ReturnType} {method.Name}({parameters})");
//             sb.AppendLine("        {");
//             sb.AppendLine($"            var cacheKey = {cacheKey};");
//             sb.AppendLine("            if (_cache.TryGetValue(cacheKey, out var cachedResult))");
//             sb.AppendLine("            {");
//             sb.AppendLine($"                return ({method.ReturnType})cachedResult;");
//             sb.AppendLine("            }");
//             sb.AppendLine();
//             sb.AppendLine($"            var result = _inner.{method.Name}({arguments});");
//             sb.AppendLine($"            _cache.Set(cacheKey, result, TimeSpan.FromMinutes({decorator.CacheExpirationMinutes}));");
//             sb.AppendLine("            return result;");
//         }
//         
//         sb.AppendLine("        }");
//         sb.AppendLine();
//     }
//
//     static void GenerateValidationDecorator(StringBuilder sb, InterfaceToDecorate iface)
//     {
//         var accessibility = iface.Accessibility.ToString().ToLower();
//         
//         sb.AppendLine($"    {accessibility} class {iface.BaseName}ValidationDecorator : {iface.BaseName}DecoratorBase");
//         sb.AppendLine("    {");
//         sb.AppendLine($"        public {iface.BaseName}ValidationDecorator({iface.Name} inner)");
//         sb.AppendLine("            : base(inner)");
//         sb.AppendLine("        {");
//         sb.AppendLine("        }");
//         sb.AppendLine();
//
//         foreach (var method in iface.Methods.Where(m => m.Decorators.Any(d => d.Type == DecoratorType.Validation)))
//         {
//             GenerateValidationMethod(sb, method);
//         }
//
//         sb.AppendLine("    }");
//         sb.AppendLine();
//     }
//
//     static void GenerateValidationMethod(StringBuilder sb, MethodToDecorate method)
//     {
//         var parameters = string.Join(", ", method.Parameters.Select(p => $"{p.Type} {p.Name}"));
//         var arguments = string.Join(", ", method.Parameters.Select(p => p.Name));
//
//         sb.AppendLine($"        public override {method.ReturnType} {method.Name}({parameters})");
//         sb.AppendLine("        {");
//         
//         // Generate null checks for reference type parameters
//         foreach (var param in method.Parameters.Where(p => !p.Type.EndsWith("?")))
//         {
//             if (param.Type == "string" || param.Type.Contains("[]") || param.Type.Contains("List") || param.Type.Contains("IEnumerable"))
//             {
//                 sb.AppendLine($"            if ({param.Name} == null) throw new ArgumentNullException(nameof({param.Name}));");
//             }
//         }
//
//         sb.AppendLine($"            return _inner.{method.Name}({arguments});");
//         sb.AppendLine("        }");
//         sb.AppendLine();
//     }
//
//     static void GenerateRetryDecorator(StringBuilder sb, InterfaceToDecorate iface)
//     {
//         var accessibility = iface.Accessibility.ToString().ToLower();
//         
//         sb.AppendLine($"    {accessibility} class {iface.BaseName}RetryDecorator : {iface.BaseName}DecoratorBase");
//         sb.AppendLine("    {");
//         sb.AppendLine($"        public {iface.BaseName}RetryDecorator({iface.Name} inner)");
//         sb.AppendLine("            : base(inner)");
//         sb.AppendLine("        {");
//         sb.AppendLine("        }");
//         sb.AppendLine();
//
//         foreach (var method in iface.Methods.Where(m => m.Decorators.Any(d => d.Type == DecoratorType.Retry)))
//         {
//             GenerateRetryMethod(sb, method);
//         }
//
//         sb.AppendLine("    }");
//         sb.AppendLine();
//     }
//
//     static void GenerateRetryMethod(StringBuilder sb, MethodToDecorate method)
//     {
//         var parameters = string.Join(", ", method.Parameters.Select(p => $"{p.Type} {p.Name}"));
//         var arguments = string.Join(", ", method.Parameters.Select(p => p.Name));
//         var decorator = method.Decorators.First(d => d.Type == DecoratorType.Retry);
//
//         if (method.IsAsync)
//         {
//             sb.AppendLine($"        public override async {method.ReturnType} {method.Name}({parameters})");
//             sb.AppendLine("        {");
//             sb.AppendLine($"            var maxAttempts = {decorator.RetryAttempts};");
//             sb.AppendLine("            for (int attempt = 1; attempt <= maxAttempts; attempt++)");
//             sb.AppendLine("            {");
//             sb.AppendLine("                try");
//             sb.AppendLine("                {");
//             
//             if (method.ReturnType == "Task")
//             {
//                 sb.AppendLine($"                    await _inner.{method.Name}({arguments});");
//                 sb.AppendLine("                    return;");
//             }
//             else
//             {
//                 sb.AppendLine($"                    return await _inner.{method.Name}({arguments});");
//             }
//             
//             sb.AppendLine("                }");
//             sb.AppendLine("                catch (Exception) when (attempt < maxAttempts)");
//             sb.AppendLine("                {");
//             sb.AppendLine("                    await Task.Delay(TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100));");
//             sb.AppendLine("                }");
//             sb.AppendLine("            }");
//             
//             if (method.ReturnType == "Task")
//             {
//                 sb.AppendLine($"            await _inner.{method.Name}({arguments});");
//             }
//             else
//             {
//                 sb.AppendLine($"            return await _inner.{method.Name}({arguments});");
//             }
//         }
//         else
//         {
//             sb.AppendLine($"        public override {method.ReturnType} {method.Name}({parameters})");
//             sb.AppendLine("        {");
//             sb.AppendLine($"            var maxAttempts = {decorator.RetryAttempts};");
//             sb.AppendLine("            for (int attempt = 1; attempt <= maxAttempts; attempt++)");
//             sb.AppendLine("            {");
//             sb.AppendLine("                try");
//             sb.AppendLine("                {");
//             sb.AppendLine($"                    return _inner.{method.Name}({arguments});");
//             sb.AppendLine("                }");
//             sb.AppendLine("                catch (Exception) when (attempt < maxAttempts)");
//             sb.AppendLine("                {");
//             sb.AppendLine("                    System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100));");
//             sb.AppendLine("                }");
//             sb.AppendLine("            }");
//             sb.AppendLine($"            return _inner.{method.Name}({arguments});");
//         }
//         
//         sb.AppendLine("        }");
//         sb.AppendLine();
//     }
//
//     static void GeneratePerformanceDecorator(StringBuilder sb, InterfaceToDecorate iface)
//     {
//         var accessibility = iface.Accessibility.ToString().ToLower();
//         
//         sb.AppendLine($"    {accessibility} class {iface.BaseName}PerformanceDecorator : {iface.BaseName}DecoratorBase");
//         sb.AppendLine("    {");
//         sb.AppendLine("        private readonly ILogger _logger;");
//         sb.AppendLine();
//         sb.AppendLine($"        public {iface.BaseName}PerformanceDecorator({iface.Name} inner, ILogger logger)");
//         sb.AppendLine("            : base(inner)");
//         sb.AppendLine("        {");
//         sb.AppendLine("            _logger = logger ?? throw new ArgumentNullException(nameof(logger));");
//         sb.AppendLine("        }");
//         sb.AppendLine();
//
//         foreach (var method in iface.Methods.Where(m => m.Decorators.Any(d => d.Type == DecoratorType.Performance)))
//         {
//             GeneratePerformanceMethod(sb, method);
//         }
//
//         sb.AppendLine("    }");
//         sb.AppendLine();
//     }
//
//     static void GeneratePerformanceMethod(StringBuilder sb, MethodToDecorate method)
//     {
//         var parameters = string.Join(", ", method.Parameters.Select(p => $"{p.Type} {p.Name}"));
//         var arguments = string.Join(", ", method.Parameters.Select(p => p.Name));
//
//         if (method.IsAsync)
//         {
//             sb.AppendLine($"        public override async {method.ReturnType} {method.Name}({parameters})");
//             sb.AppendLine("        {");
//             sb.AppendLine("            var stopwatch = Stopwatch.StartNew();");
//             sb.AppendLine("            try");
//             sb.AppendLine("            {");
//             
//             if (method.ReturnType == "Task")
//             {
//                 sb.AppendLine($"                await _inner.{method.Name}({arguments});");
//             }
//             else
//             {
//                 sb.AppendLine($"                var result = await _inner.{method.Name}({arguments});");
//                 sb.AppendLine("                return result;");
//             }
//             
//             sb.AppendLine("            }");
//             sb.AppendLine("            finally");
//             sb.AppendLine("            {");
//             sb.AppendLine("                stopwatch.Stop();");
//             sb.AppendLine($"                _logger.LogInformation(\"{method.Name} executed in {{ElapsedMs}}ms\", stopwatch.ElapsedMilliseconds);");
//             sb.AppendLine("            }");
//         }
//         else
//         {
//             sb.AppendLine($"        public override {method.ReturnType} {method.Name}({parameters})");
//             sb.AppendLine("        {");
//             sb.AppendLine("            var stopwatch = Stopwatch.StartNew();");
//             sb.AppendLine("            try");
//             sb.AppendLine("            {");
//             sb.AppendLine($"                return _inner.{method.Name}({arguments});");
//             sb.AppendLine("            }");
//             sb.AppendLine("            finally");
//             sb.AppendLine("            {");
//             sb.AppendLine("                stopwatch.Stop();");
//             sb.AppendLine($"                _logger.LogInformation(\"{method.Name} executed in {{ElapsedMs}}ms\", stopwatch.ElapsedMilliseconds);");
//             sb.AppendLine("            }");
//         }
//         
//         sb.AppendLine("        }");
//         sb.AppendLine();
//     }
//
//     static void GenerateDecoratorFactory(StringBuilder sb, InterfaceToDecorate iface)
//     {
//         var accessibility = iface.Accessibility.ToString().ToLower();
//         
//         sb.AppendLine($"    {accessibility} static class {iface.BaseName}DecoratorFactory");
//         sb.AppendLine("    {");
//         sb.AppendLine($"        public static {iface.Name} Create({iface.Name} implementation)");
//         sb.AppendLine("        {");
//         sb.AppendLine("            return implementation;");
//         sb.AppendLine("        }");
//         sb.AppendLine();
//         sb.AppendLine($"        public static {iface.Name} WithLogging(this {iface.Name} service, ILogger logger)");
//         sb.AppendLine("        {");
//         sb.AppendLine($"            return new {iface.BaseName}LoggingDecorator(service, logger);");
//         sb.AppendLine("        }");
//         sb.AppendLine();
//         sb.AppendLine($"        public static {iface.Name} WithCaching(this {iface.Name} service, IMemoryCache cache)");
//         sb.AppendLine("        {");
//         sb.AppendLine($"            return new {iface.BaseName}CachingDecorator(service, cache);");
//         sb.AppendLine("        }");
//         sb.AppendLine();
//         sb.AppendLine($"        public static {iface.Name} WithValidation(this {iface.Name} service)");
//         sb.AppendLine("        {");
//         sb.AppendLine($"            return new {iface.BaseName}ValidationDecorator(service);");
//         sb.AppendLine("        }");
//         sb.AppendLine();
//         sb.AppendLine($"        public static {iface.Name} WithRetry(this {iface.Name} service)");
//         sb.AppendLine("        {");
//         sb.AppendLine($"            return new {iface.BaseName}RetryDecorator(service);");
//         sb.AppendLine("        }");
//         sb.AppendLine();
//         sb.AppendLine($"        public static {iface.Name} WithPerformanceMonitoring(this {iface.Name} service, ILogger logger)");
//         sb.AppendLine("        {");
//         sb.AppendLine($"            return new {iface.BaseName}PerformanceDecorator(service, logger);");
//         sb.AppendLine("        }");
//         sb.AppendLine("    }");
//     }
//
//     // Helper methods for attribute extraction
//     static DecoratorAttribute? GetDecoratorAttribute(INamedTypeSymbol type)
//     {
//         foreach (var attr in type.GetAttributes())
//         {
//             string attributeName = attr.AttributeClass?.Name ?? string.Empty;
//             if (attributeName == "GenerateDecoratorsAttribute" || attributeName == "GenerateDecorators")
//             {
//                 var result = new DecoratorAttribute();
//
//                 foreach (var namedArg in attr.NamedArguments)
//                 {
//                     switch (namedArg.Key)
//                     {
//                         case nameof(DecoratorAttribute.BaseName):
//                             result.BaseName = (string?)namedArg.Value.Value;
//                             break;
//                         case nameof(DecoratorAttribute.GenerateFactory):
//                             result.GenerateFactory = (bool)namedArg.Value.Value!;
//                             break;
//                         case nameof(DecoratorAttribute.GenerateAsyncSupport):
//                             result.GenerateAsyncSupport = (bool)namedArg.Value.Value!;
//                             break;
//                         case nameof(DecoratorAttribute.Accessibility):
//                             result.Accessibility = (DecoratorAccessibility)namedArg.Value.Value!;
//                             break;
//                     }
//                 }
//
//                 return result;
//             }
//         }
//
//         return null;
//     }
//
//     static List<DecoratorTypeAttribute> GetMethodDecorators(IMethodSymbol method)
//     {
//         var decorators = new List<DecoratorTypeAttribute>();
//
//         foreach (var attr in method.GetAttributes())
//         {
//             string attributeName = attr.AttributeClass?.Name ?? string.Empty;
//             if (attributeName == "DecoratorAttribute" || attributeName == "Decorator")
//             {
//                 var decorator = new DecoratorTypeAttribute();
//
//                 foreach (var namedArg in attr.NamedArguments)
//                 {
//                     switch (namedArg.Key)
//                     {
//                         case nameof(DecoratorTypeAttribute.Type):
//                             decorator.Type = (DecoratorType)namedArg.Value.Value!;
//                             break;
//                         case nameof(DecoratorTypeAttribute.Name):
//                             decorator.Name = (string?)namedArg.Value.Value;
//                             break;
//                         case nameof(DecoratorTypeAttribute.CacheExpirationMinutes):
//                             decorator.CacheExpirationMinutes = (int)namedArg.Value.Value!;
//                             break;
//                         case nameof(DecoratorTypeAttribute.RetryAttempts):
//                             decorator.RetryAttempts = (int)namedArg.Value.Value!;
//                             break;
//                     }
//                 }
//
//                 decorators.Add(decorator);
//             }
//         }
//
//         return decorators;
//     }
// }
