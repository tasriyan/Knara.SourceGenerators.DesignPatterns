using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using VerifyXunit;
using CodeGenerator.Patterns.Decorator;

namespace CodeGenerator.Patterns.Decorator.Tests;

public class DecoratorFactoryGeneratorTests
{
    [Fact]
    public async Task GenerateDecoratorFactory_SimpleInterface_GeneratesFactory()
    {
        var source = @"
using CodeGenerator.Patterns.Decorator;

namespace TestNamespace
{
    [GenerateDecoratorFactory]
    public interface IService
    {
        void DoWork();
    }

    [Decorator(Type = ""Logging"")]
    public class LoggingDecorator : IService
    {
        private readonly IService _service;

        public LoggingDecorator(IService service)
        {
            _service = service;
        }

        public void DoWork() => _service.DoWork();
    }
}";

        var result = await RunGenerator(source);
        await Verify(result).UseMethodName("SimpleInterface");
    }

    [Fact]
    public async Task GenerateDecoratorFactory_CustomBaseName_UsesCustomName()
    {
        var source = @"
using CodeGenerator.Patterns.Decorator;

namespace TestNamespace
{
    [GenerateDecoratorFactory(BaseName = ""CustomService"")]
    public interface IMyService
    {
        string GetData();
    }

    [Decorator(Type = ""Cache"")]
    public class CacheDecorator : IMyService
    {
        private readonly IMyService _service;

        public CacheDecorator(IMyService service)
        {
            _service = service;
        }

        public string GetData() => _service.GetData();
    }
}";

        var result = await RunGenerator(source);
        await Verify(result).UseMethodName("CustomBaseName");
    }

    [Fact]
    public async Task GenerateDecoratorFactory_DecoratorWithAdditionalParameters_GeneratesCorrectSignature()
    {
        var source = @"
using CodeGenerator.Patterns.Decorator;

namespace TestNamespace
{
    [GenerateDecoratorFactory]
    public interface IRepository
    {
        void Save(string data);
    }

    [Decorator(Type = ""Retry"")]
    public class RetryDecorator : IRepository
    {
        private readonly IRepository _repository;
        private readonly int _maxAttempts;
        private readonly string _logPrefix;

        public RetryDecorator(IRepository repository, int maxAttempts, string logPrefix)
        {
            _repository = repository;
            _maxAttempts = maxAttempts;
            _logPrefix = logPrefix;
        }

        public void Save(string data) => _repository.Save(data);
    }
}";

        var result = await RunGenerator(source);
        await Verify(result).UseMethodName("AdditionalParameters");
    }

    [Fact]
    public async Task GenerateDecoratorFactory_MultipleDecorators_GeneratesAllExtensionMethods()
    {
        var source = @"
using CodeGenerator.Patterns.Decorator;

namespace TestNamespace
{
    [GenerateDecoratorFactory]
    public interface IProcessor
    {
        void Process();
    }

    [Decorator(Type = ""Validation"")]
    public class ValidationDecorator : IProcessor
    {
        private readonly IProcessor _processor;

        public ValidationDecorator(IProcessor processor)
        {
            _processor = processor;
        }

        public void Process() => _processor.Process();
    }

    [Decorator(Type = ""Timing"")]
    public class TimingDecorator : IProcessor
    {
        private readonly IProcessor _processor;
        private readonly bool _logToConsole;

        public TimingDecorator(IProcessor processor, bool logToConsole)
        {
            _processor = processor;
            _logToConsole = logToConsole;
        }

        public void Process() => _processor.Process();
    }
}";

        var result = await RunGenerator(source);
        await Verify(result).UseMethodName("MultipleDecorators");
    }

    [Fact]
    public async Task GenerateDecoratorFactory_NoDecorators_GeneratesNothing()
    {
        var source = @"
using CodeGenerator.Patterns.Decorator;

namespace TestNamespace
{
    [GenerateDecoratorFactory]
    public interface IService
    {
        void DoWork();
    }
}";

        var result = await RunGenerator(source);
        await Verify(result).UseMethodName("NoDecorators");
    }

    [Fact]
    public async Task GenerateDecoratorFactory_InterfaceWithoutAttribute_NoGeneration()
    {
        var source = @"
using CodeGenerator.Patterns.Decorator;

namespace TestNamespace
{
    public interface IService
    {
        void DoWork();
    }

    [Decorator(Type = ""Logging"")]
    public class LoggingDecorator : IService
    {
        private readonly IService _service;

        public LoggingDecorator(IService service)
        {
            _service = service;
        }

        public void DoWork() => _service.DoWork();
    }
}";

        var result = await RunGenerator(source);
        await Verify(result).UseMethodName("NoAttribute");
    }

    [Fact]
    public async Task GenerateDecoratorFactory_DecoratorWithoutTypeAttribute_NotIncluded()
    {
        var source = @"
using CodeGenerator.Patterns.Decorator;

namespace TestNamespace
{
    [GenerateDecoratorFactory]
    public interface IService
    {
        void DoWork();
    }

    [Decorator]
    public class InvalidDecorator : IService
    {
        private readonly IService _service;

        public InvalidDecorator(IService service)
        {
            _service = service;
        }

        public void DoWork() => _service.DoWork();
    }

    [Decorator(Type = ""Valid"")]
    public class ValidDecorator : IService
    {
        private readonly IService _service;

        public ValidDecorator(IService service)
        {
            _service = service;
        }

        public void DoWork() => _service.DoWork();
    }
}";

        var result = await RunGenerator(source);
        await Verify(result).UseMethodName("InvalidDecorator");
    }

    [Fact]
    public async Task GenerateDecoratorFactory_NoNamespace_GeneratesWithoutNamespace()
    {
        var source = @"
using CodeGenerator.Patterns.Decorator;

[GenerateDecoratorFactory]
public interface IGlobalService
{
    void Execute();
}

[Decorator(Type = ""Global"")]
public class GlobalDecorator : IGlobalService
{
    private readonly IGlobalService _service;

    public GlobalDecorator(IGlobalService service)
    {
        _service = service;
    }

    public void Execute() => _service.Execute();
}";

        var result = await RunGenerator(source);
        await Verify(result).UseMethodName("NoNamespace");
    }

    private static async Task<GeneratorResult> RunGenerator(string source)
    {
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { CSharpSyntaxTree.ParseText(source) },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Attribute).Assembly.Location)
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new DecoratorFactoryGenerator();
        
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation) as CSharpGeneratorDriver;

        var result = driver.GetRunResult();
        
        return new GeneratorResult
        {
            GeneratedSources = result.Results[0].GeneratedSources
                .Select(gs => new GeneratedSource 
                { 
                    HintName = gs.HintName, 
                    SourceText = gs.SourceText.ToString() 
                })
                .ToArray(),
            Diagnostics = result.Diagnostics.ToArray()
        };
    }

    public class GeneratorResult
    {
        public GeneratedSource[] GeneratedSources { get; set; } = Array.Empty<GeneratedSource>();
        public Diagnostic[] Diagnostics { get; set; } = Array.Empty<Diagnostic>();
    }

    public class GeneratedSource
    {
        public string HintName { get; set; } = string.Empty;
        public string SourceText { get; set; } = string.Empty;
    }
}