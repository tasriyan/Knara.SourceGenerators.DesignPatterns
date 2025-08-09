using CodeGenerator.Patterns.Mediator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using System.Linq;
using Xunit;

namespace Mediator.UnitTests;

public class DeclarativeMediatorGeneratorTests
{
    private static CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.IAsyncEnumerable<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Runtime.CompilerServices.EnumeratorCancellationAttribute).Assembly.Location)
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    [Fact]
    public void Initialize_ShouldAddMediatorAttributes()
    {
        // Arrange
        var generator = new DeclarativeMediatorGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var compilation = CreateCompilation("");

        // Act
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics) as CSharpGeneratorDriver;
        var runResult = driver.GetRunResult();

        // Assert
        runResult.Results.Length.ShouldBe(1);
        var attributesSource = runResult.Results[0].GeneratedSources
            .First(s => s.HintName == "MediatorAttributes.g.cs").SourceText.ToString();

        attributesSource.ShouldContain("public class QueryAttribute");
        attributesSource.ShouldContain("public class CommandAttribute");
        attributesSource.ShouldContain("public class StreamQueryAttribute");
        attributesSource.ShouldContain("public interface IMediator");
        attributesSource.ShouldContain("public interface IQuery<out TResponse>");
    }

    [Fact]
    public void Generate_QueryClass_ShouldCreateRequestAndHandler()
    {
        // Arrange
        const string source = @"
using CodeGenerator.Patterns.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Query(Name = ""GetUserQuery"", ResponseType = typeof(string))]
    public class GetUserDto
    {
        public int UserId { get; set; }
        public string Name { get; set; } = """";
    }

    [QueryHandler(Name = ""GetUserHandler"", RequestType = typeof(GetUserQuery))]
    public class UserService
    {
        public async Task<string> GetAsync(GetUserQuery request, CancellationToken cancellationToken = default)
        {
            return $""User {request.Name} with ID {request.UserId}"";
        }
    }
}";

        var generator = new DeclarativeMediatorGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var compilation = CreateCompilation(source);

        // Act
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics) as CSharpGeneratorDriver;
        var runResult = driver.GetRunResult();

        // Assert
        runResult.Diagnostics.ShouldBeEmpty();
        var generatedSources = runResult.Results[0].GeneratedSources;
        
        generatedSources.ShouldContain(s => s.HintName == "GetUserQuery.Request.g.cs");
        generatedSources.ShouldContain(s => s.HintName == "GetUserHandler.Handler.g.cs");
        generatedSources.ShouldContain(s => s.HintName == "GeneratedMediator.g.cs");

        var requestSource = generatedSources.First(s => s.HintName == "GetUserQuery.Request.g.cs").SourceText.ToString();
        requestSource.ShouldContain("public class GetUserQuery : CodeGenerator.Patterns.Mediator.IQuery<string>");
        requestSource.ShouldContain("public int UserId { get; set; }");
        requestSource.ShouldContain("public string Name { get; set; } = \"\";");
    }

    [Fact]
    public void Generate_CommandClass_ShouldCreateRequestAndHandler()
    {
        // Arrange
        const string source = @"
using CodeGenerator.Patterns.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Command(Name = ""CreateUser"")]
    public class CreateUserCommand
    {
        public string Name { get; set; } = """";
        public int Age { get; set; }
    }

    [CommandHandler(Name = ""CreateUser"", RequestType = typeof(CreateUserCommand))]
    public class UserService
    {
        public async Task CreateAsync(CreateUserCommand request, CancellationToken cancellationToken = default)
        {
        }
    }
}";

        var generator = new DeclarativeMediatorGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var compilation = CreateCompilation(source);

        // Act
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics) as CSharpGeneratorDriver;
        var runResult = driver.GetRunResult();

        // Assert
        var generatedSources = runResult.Results[0].GeneratedSources;
        var requestSource = generatedSources.First(s => s.HintName == "CreateUser.Request.g.cs").SourceText.ToString();
        
        requestSource.ShouldContain("public class CreateUser : CodeGenerator.Patterns.Mediator.ICommand");
        requestSource.ShouldContain("public string Name { get; set; } = \"\";");
        requestSource.ShouldContain("public int Age { get; set; }");
    }

    [Fact]
    public void Generate_CommandWithResponse_ShouldCreateRequestAndHandler()
    {
        // Arrange
        const string source = @"
using CodeGenerator.Patterns.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Command(Name = ""CreateUser"", ResponseType = typeof(int))]
    public class CreateUserCommand
    {
        public string Name { get; set; } = """";
    }

    [CommandHandler(Name = ""CreateUser"", RequestType = typeof(CreateUserCommand))]
    public class UserService
    {
        public async Task<int> CreateAsync(CreateUserCommand request, CancellationToken cancellationToken = default)
        {
            return 123;
        }
    }
}";

        var generator = new DeclarativeMediatorGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var compilation = CreateCompilation(source);

        // Act
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics) as CSharpGeneratorDriver;
        var runResult = driver.GetRunResult();

        // Assert
        var generatedSources = runResult.Results[0].GeneratedSources;
        var requestSource = generatedSources.First(s => s.HintName == "CreateUser.Request.g.cs").SourceText.ToString();
        
        requestSource.ShouldContain("public class CreateUser : CodeGenerator.Patterns.Mediator.ICommand<int>");
    }

    [Fact]
    public void Generate_StreamQuery_ShouldCreateRequestAndHandler()
    {
        // Arrange
        const string source = @"
using CodeGenerator.Patterns.Mediator;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    [StreamQuery(Name = ""GetUsers"", ResponseType = typeof(string))]
    public class GetUsersQuery
    {
        public int PageSize { get; set; }
    }

    [StreamQueryHandler(Name = ""GetUsers"", RequestType = typeof(GetUsersQuery))]
    public class UserService
    {
        public async IAsyncEnumerable<string> GetAsync(GetUsersQuery request, CancellationToken cancellationToken = default)
        {
            yield return ""User1"";
        }
    }
}";

        var generator = new DeclarativeMediatorGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var compilation = CreateCompilation(source);

        // Act
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics) as CSharpGeneratorDriver;
        var runResult = driver.GetRunResult();

        // Assert
        var generatedSources = runResult.Results[0].GeneratedSources;
        var requestSource = generatedSources.First(s => s.HintName == "GetUsers.Request.g.cs").SourceText.ToString();
        
        requestSource.ShouldContain("public class GetUsers : CodeGenerator.Patterns.Mediator.IStreamQuery<string>");
    }

    [Fact]
    public void Generate_RecordQuery_ShouldCreateRequestAndHandler()
    {
        // Arrange
        const string source = @"
using CodeGenerator.Patterns.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Query(Name = ""GetUser"", ResponseType = typeof(string))]
    public record GetUserQuery(int UserId, string Name);

    [QueryHandler(Name = ""GetUser"", RequestType = typeof(GetUserQuery))]
    public class UserService
    {
        public async Task<string> GetAsync(GetUserQuery request, CancellationToken cancellationToken = default)
        {
            return $""User {request.Name}"";
        }
    }
}";

        var generator = new DeclarativeMediatorGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var compilation = CreateCompilation(source);

        // Act
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics) as CSharpGeneratorDriver;
        var runResult = driver.GetRunResult();

        // Assert
        var generatedSources = runResult.Results[0].GeneratedSources;
        var requestSource = generatedSources.First(s => s.HintName == "GetUser.Request.g.cs").SourceText.ToString();
        
        requestSource.ShouldContain("public class GetUser : CodeGenerator.Patterns.Mediator.IQuery<string>");
        requestSource.ShouldContain("public int UserId { get; set; }");
        requestSource.ShouldContain("public string Name { get; set; } = \"\";");
    }

    [Fact]
    public void Generate_LegacyMethodHandler_ShouldCreateRequestAndHandler()
    {
        // Arrange
        const string source = @"
using CodeGenerator.Patterns.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    public class UserService
    {
        [RequestHandler(Name = ""ProcessData"")]
        public async Task<string> ProcessDataAsync(int id, string name, CancellationToken cancellationToken = default)
        {
            return $""Processed {name} with ID {id}"";
        }
    }
}";

        var generator = new DeclarativeMediatorGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var compilation = CreateCompilation(source);

        // Act
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics) as CSharpGeneratorDriver;
        var runResult = driver.GetRunResult();

        // Assert
        var generatedSources = runResult.Results[0].GeneratedSources;
        
        generatedSources.ShouldContain(s => s.HintName.Contains("Request.g.cs"));
        generatedSources.ShouldContain(s => s.HintName.Contains("Handler.g.cs"));

        var requestSource = generatedSources.First(s => s.HintName.Contains("Request.g.cs")).SourceText.ToString();
        requestSource.ShouldContain("IRequest<string>");
        requestSource.ShouldContain("public int Id { get; set; }");
        requestSource.ShouldContain("public string Name { get; set; } = \"\";");
    }

    [Fact]
    public void Generate_NotificationHandler_ShouldCreateHandler()
    {
        // Arrange
        const string source = @"
using CodeGenerator.Patterns.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    public class UserCreatedEvent
    {
        public int UserId { get; set; }
    }

    [NotificationHandler(Name = ""EmailNotificationHandler"", EventType = typeof(UserCreatedEvent))]
    public class EmailService
    {
        public async Task ProcessAsync(UserCreatedEvent eventObj, CancellationToken cancellationToken = default)
        {
        }
    }
}";

        var generator = new DeclarativeMediatorGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var compilation = CreateCompilation(source);

        // Act
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics) as CSharpGeneratorDriver;
        var runResult = driver.GetRunResult();

        // Assert
        var generatedSources = runResult.Results[0].GeneratedSources;
        var handlerSource = generatedSources.First(s => s.HintName.Contains("Handler.g.cs")).SourceText.ToString();
        
        handlerSource.ShouldContain("public class EmailNotificationHandler");
        handlerSource.ShouldContain("UserCreatedEvent eventObj");
    }

    [Fact]
    public void Generate_MediatorImplementation_ShouldContainAllMethods()
    {
        // Arrange
        const string source = @"
using CodeGenerator.Patterns.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Query(Name = ""GetUser"", ResponseType = typeof(string))]
    public class GetUserQuery { public int Id { get; set; } }

    [QueryHandler(Name = ""GetUser"", RequestType = typeof(GetUserQuery))]
    public class UserService 
    {
        public async Task<string> GetAsync(GetUserQuery request, CancellationToken cancellationToken = default) => """";
    }
}";

        var generator = new DeclarativeMediatorGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var compilation = CreateCompilation(source);

        // Act
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics) as CSharpGeneratorDriver;
        var runResult = driver.GetRunResult();

        // Assert
        var generatedSources = runResult.Results[0].GeneratedSources;
        var mediatorSource = generatedSources.First(s => s.HintName == "GeneratedMediator.g.cs").SourceText.ToString();
        
        mediatorSource.ShouldContain("public sealed class GeneratedMediator : CodeGenerator.Patterns.Mediator.IMediator");
        mediatorSource.ShouldContain("Task<TResponse> Send<TResponse>(CodeGenerator.Patterns.Mediator.IQuery<TResponse>");
        mediatorSource.ShouldContain("Task Send(CodeGenerator.Patterns.Mediator.ICommand command");
        mediatorSource.ShouldContain("Task<TResponse> Send<TResponse>(CodeGenerator.Patterns.Mediator.ICommand<TResponse>");
        mediatorSource.ShouldContain("Task Publish<TEvent>");
        mediatorSource.ShouldContain("IAsyncEnumerable<TResponse> CreateStream<TResponse>");
        mediatorSource.ShouldContain("Task Send(CodeGenerator.Patterns.Mediator.IRequest request"); // Legacy support
        mediatorSource.ShouldContain("Task<TResponse> Send<TResponse>(CodeGenerator.Patterns.Mediator.IRequest<TResponse>"); // Legacy support
    }

    [Fact]
    public void Generate_DIExtensions_ShouldRegisterAllServices()
    {
        // Arrange
        const string source = @"
using CodeGenerator.Patterns.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Query(Name = ""GetUser"", ResponseType = typeof(string))]
    public class GetUserQuery { }

    [QueryHandler(Name = ""GetUser"", RequestType = typeof(GetUserQuery))]
    public class UserService 
    {
        public async Task<string> GetAsync(GetUserQuery request, CancellationToken cancellationToken = default) => """";
    }
}";

        var generator = new DeclarativeMediatorGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var compilation = CreateCompilation(source);

        // Act
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics) as CSharpGeneratorDriver;
        var runResult = driver.GetRunResult();

        // Assert
        var generatedSources = runResult.Results[0].GeneratedSources;
        var diSource = generatedSources.First(s => s.HintName == "MediatorDIExtensions.g.cs").SourceText.ToString();
        
        diSource.ShouldContain("AddDeclarativeMediator");
        diSource.ShouldContain("AddScoped<TestNamespace.UserService>");
        diSource.ShouldContain("AddSingleton<IMediator, GeneratedMediator>");
    }

    [Theory]
    [InlineData("string")]
    [InlineData("int")]
    [InlineData("bool")]
    [InlineData("System.DateTime")]
    [InlineData("System.Generic.Collections.List<string>")]
    public void Generate_PropertyTypes_ShouldHandleCorrectly(string propertyType)
    {
        // Arrange
        var source = $@"
using CodeGenerator.Patterns.Mediator;
using System;
using System.Collections.Generic;

namespace TestNamespace
{{
    [Query(Name = ""TestQuery"", ResponseType = typeof(string))]
    public class TestRequest
    {{
        public {propertyType} TestProperty {{ get; set; }}
    }}
}}";

        var generator = new DeclarativeMediatorGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var compilation = CreateCompilation(source);

        // Act
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics) as CSharpGeneratorDriver;
        var runResult = driver.GetRunResult();

        // Assert
        var generatedSources = runResult.Results[0].GeneratedSources;
        var requestSource = generatedSources.First(s => s.HintName == "TestQuery.Request.g.cs").SourceText.ToString();
        
        requestSource.ShouldContain($"public {propertyType} TestProperty {{ get; set; }}");
    }

    [Fact]
    public void Generate_MissingName_ShouldReportDiagnostic()
    {
        // Arrange
        const string source = @"
using CodeGenerator.Patterns.Mediator;

namespace TestNamespace
{
    [Query]
    public class GetUserQuery
    {
        public int Id { get; set; }
    }
}";

        var generator = new DeclarativeMediatorGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var compilation = CreateCompilation(source);

        // Act
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics) as CSharpGeneratorDriver;
        var runResult = driver.GetRunResult();

        // Assert
        runResult.Diagnostics.ShouldNotBeEmpty();
        runResult.Diagnostics.ShouldContain(d => d.Id == "MED001");
    }

    [Fact]
    public void Generate_ConflictingAttributes_ShouldReportDiagnostic()
    {
        // Arrange
        const string source = @"
using CodeGenerator.Patterns.Mediator;

namespace TestNamespace
{
    [Query(Name = ""GetUser"")]
    [Command(Name = ""GetUser"")]
    public class GetUserQuery
    {
        public int Id { get; set; }
    }
}";

        var generator = new DeclarativeMediatorGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var compilation = CreateCompilation(source);

        // Act
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics) as CSharpGeneratorDriver;
        var runResult = driver.GetRunResult();

        // Assert
        runResult.Diagnostics.ShouldNotBeEmpty();
        runResult.Diagnostics.ShouldContain(d => d.Id == "MED006");
    }
}