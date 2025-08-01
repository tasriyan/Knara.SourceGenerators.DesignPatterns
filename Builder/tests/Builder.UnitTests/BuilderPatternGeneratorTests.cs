using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SourceGenerators.DesignPatterns.Builder.Tests;

public class BuilderPatternGeneratorTests
{
    private static GeneratorDriver CreateDriver()
    {
        return CSharpGeneratorDriver.Create(new BuilderPatternGenerator());
    }

    private static Compilation CreateCompilation(string source)
    {
        return CSharpCompilation.Create("TestAssembly",
            [CSharpSyntaxTree.ParseText(source)],
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
            ],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    [Fact]
    public void Should_Generate_Attributes_Source()
    {
        // Arrange
        var driver = CreateDriver();
        var compilation = CreateCompilation("");

        // Act
        var result = driver.RunGenerators(compilation).GetRunResult();

        // Assert
        result.Diagnostics.ShouldBeEmpty();
        result.GeneratedTrees.Length.ShouldBe(1);
        
        var attributesSource = result.GeneratedTrees[0].ToString();
        attributesSource.ShouldContain("GenerateBuilderAttribute");
        attributesSource.ShouldContain("BuilderPropertyAttribute");
        attributesSource.ShouldContain("BuilderCollectionAttribute");
        attributesSource.ShouldContain("BuilderAccessibility");
    }

    [Fact]
    public void Should_Generate_Builder_For_Simple_Class()
    {
        // Arrange
        var source = """
            using SourceGenerators.DesignPatterns.Builder;

            [GenerateBuilder]
            public class Person
            {
                public string FirstName { get; set; } = "";
                public string LastName { get; set; } = "";
                public int Age { get; set; }
            }
            """;

        var driver = CreateDriver();
        var compilation = CreateCompilation(source);

        // Act
        var result = driver.RunGenerators(compilation).GetRunResult();

        // Assert
        result.Diagnostics.ShouldBeEmpty();
        result.GeneratedTrees.Length.ShouldBe(2); // Attributes + PersonBuilder

        var builderSource = result.GeneratedTrees
            .First(t => t.FilePath.Contains("PersonBuilder")).ToString();

        builderSource.ShouldContain("public sealed class PersonBuilder");
        builderSource.ShouldContain("public static PersonBuilder Create()");
        builderSource.ShouldContain("public PersonBuilder WithFirstName(string firstName)");
        builderSource.ShouldContain("public PersonBuilder WithLastName(string lastName)");
        builderSource.ShouldContain("public PersonBuilder WithAge(int age)");
        builderSource.ShouldContain("public Person Build()");
    }

    [Fact]
    public void Should_Generate_Builder_With_Required_Properties()
    {
        // Arrange
        var source = """
            using SourceGenerators.DesignPatterns.Builder;

            [GenerateBuilder]
            public class User
            {
                [BuilderProperty(Required = true)]
                public string Email { get; set; } = "";
                
                public string Name { get; set; } = "";
            }
            """;

        var driver = CreateDriver();
        var compilation = CreateCompilation(source);

        // Act
        var result = driver.RunGenerators(compilation).GetRunResult();

        // Assert
        result.Diagnostics.ShouldBeEmpty();

        var builderSource = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserBuilder")).ToString();

        builderSource.ShouldContain("private bool _emailSet = false;");
        builderSource.ShouldContain("_emailSet = true;");
        builderSource.ShouldContain("if (!_emailSet)");
        builderSource.ShouldContain("throw new InvalidOperationException(\"Required property 'Email' has not been set\");");
        builderSource.ShouldContain("public bool CanBuild()");
        builderSource.ShouldContain("public IEnumerable<string> GetMissingRequiredProperties()");
    }

    [Fact]
    public void Should_Generate_Builder_With_Collections()
    {
        // Arrange
        var source = """
            using System.Collections.Generic;
            using SourceGenerators.DesignPatterns.Builder;

            [GenerateBuilder]
            public class Order
            {
                public string OrderId { get; set; } = "";
                public List<string> Items { get; set; } = new();
            }
            """;

        var driver = CreateDriver();
        var compilation = CreateCompilation(source);

        // Act
        var result = driver.RunGenerators(compilation).GetRunResult();

        // Assert
        result.Diagnostics.ShouldBeEmpty();

        var builderSource = result.GeneratedTrees
            .First(t => t.FilePath.Contains("OrderBuilder")).ToString();

        builderSource.ShouldContain("private List<string> _items = new();");
        builderSource.ShouldContain("public OrderBuilder AddItem(string item)");
        builderSource.ShouldContain("public OrderBuilder AddItems(IEnumerable<string> items)");
        builderSource.ShouldContain("public OrderBuilder ClearItems()");
        builderSource.ShouldContain("public int ItemsCount => _items.Count;");
    }

    [Fact]
    public void Should_Generate_Builder_With_Dictionary_Properties()
    {
        // Arrange
        var source = """
            using System.Collections.Generic;
            using SourceGenerators.DesignPatterns.Builder;

            [GenerateBuilder]
            public class Configuration
            {
                public string Name { get; set; } = "";
                public Dictionary<string, string> Settings { get; set; } = new();
            }
            """;

        var driver = CreateDriver();
        var compilation = CreateCompilation(source);

        // Act
        var result = driver.RunGenerators(compilation).GetRunResult();

        // Assert
        result.Diagnostics.ShouldBeEmpty();

        var builderSource = result.GeneratedTrees
            .First(t => t.FilePath.Contains("ConfigurationBuilder")).ToString();

        builderSource.ShouldContain("private System.Collections.Generic.Dictionary<string, string> _settings");
        builderSource.ShouldContain("public ConfigurationBuilder WithSettings(System.Collections.Generic.Dictionary<string, string> settings)");
        builderSource.ShouldContain("public ConfigurationBuilder AddSetting(string key, string value)");
        builderSource.ShouldContain("public ConfigurationBuilder ClearSettings()");
        builderSource.ShouldNotContain("List<KeyValuePair<string, string>>");
    }

    [Fact]
    public void Should_Generate_Builder_With_Custom_Builder_Name()
    {
        // Arrange
        var source = """
            using SourceGenerators.DesignPatterns.Builder;

            [GenerateBuilder(BuilderName = "CustomPersonBuilder")]
            public class Person
            {
                public string Name { get; set; } = "";
            }
            """;

        var driver = CreateDriver();
        var compilation = CreateCompilation(source);

        // Act
        var result = driver.RunGenerators(compilation).GetRunResult();

        // Assert
        result.Diagnostics.ShouldBeEmpty();

        var builderSource = result.GeneratedTrees
            .First(t => t.FilePath.Contains("CustomPersonBuilder")).ToString();

        builderSource.ShouldContain("public sealed class CustomPersonBuilder");
        builderSource.ShouldContain("public static CustomPersonBuilder Create()");
    }

    [Fact]
    public void Should_Generate_From_Method_When_Enabled()
    {
        // Arrange
        var source = """
            using SourceGenerators.DesignPatterns.Builder;

            [GenerateBuilder(GenerateFromMethod = true)]
            public class Product
            {
                public string Name { get; set; } = "";
                public decimal Price { get; set; }
            }
            """;

        var driver = CreateDriver();
        var compilation = CreateCompilation(source);

        // Act
        var result = driver.RunGenerators(compilation).GetRunResult();

        // Assert
        result.Diagnostics.ShouldBeEmpty();

        var builderSource = result.GeneratedTrees
            .First(t => t.FilePath.Contains("ProductBuilder")).ToString();

        builderSource.ShouldContain("public static ProductBuilder From(Product source)");
        builderSource.ShouldContain("_name = source.Name,");
        builderSource.ShouldContain("_price = source.Price,");
    }

    [Fact]
    public void Should_Generate_Extension_Methods_When_From_Method_Enabled()
    {
        // Arrange
        var source = """
            using SourceGenerators.DesignPatterns.Builder;

            [GenerateBuilder(GenerateFromMethod = true)]
            public class Product
            {
                public string Name { get; set; } = "";
            }
            """;

        var driver = CreateDriver();
        var compilation = CreateCompilation(source);

        // Act
        var result = driver.RunGenerators(compilation).GetRunResult();

        // Assert
        result.Diagnostics.ShouldBeEmpty();

        var builderSource = result.GeneratedTrees
            .First(t => t.FilePath.Contains("ProductBuilder")).ToString();

        builderSource.ShouldContain("public static class ProductExtensions");
        builderSource.ShouldContain("public static ProductBuilder ToBuilder(this Product source)");
        builderSource.ShouldContain("ProductBuilder.From(source);");
    }

    [Fact]
    public void Should_Handle_Custom_Setter_Names()
    {
        // Arrange
        var source = """
            using SourceGenerators.DesignPatterns.Builder;

            [GenerateBuilder]
            public class Person
            {
                [BuilderProperty(CustomSetterName = "SetFullName")]
                public string Name { get; set; } = "";
            }
            """;

        var driver = CreateDriver();
        var compilation = CreateCompilation(source);

        // Act
        var result = driver.RunGenerators(compilation).GetRunResult();

        // Assert
        result.Diagnostics.ShouldBeEmpty();

        var builderSource = result.GeneratedTrees
            .First(t => t.FilePath.Contains("PersonBuilder")).ToString();

        builderSource.ShouldContain("public PersonBuilder SetFullName(string name)");
        builderSource.ShouldNotContain("WithName");
    }

    [Fact]
    public void Should_Skip_Properties_With_IgnoreInBuilder()
    {
        // Arrange
        var source = """
            using SourceGenerators.DesignPatterns.Builder;

            [GenerateBuilder]
            public class Person
            {
                public string Name { get; set; } = "";
                
                [BuilderProperty(IgnoreInBuilder = true)]
                public string InternalId { get; set; } = "";
            }
            """;

        var driver = CreateDriver();
        var compilation = CreateCompilation(source);

        // Act
        var result = driver.RunGenerators(compilation).GetRunResult();

        // Assert
        result.Diagnostics.ShouldBeEmpty();

        var builderSource = result.GeneratedTrees
            .First(t => t.FilePath.Contains("PersonBuilder")).ToString();

        builderSource.ShouldContain("WithName");
        builderSource.ShouldNotContain("InternalId");
        builderSource.ShouldNotContain("WithInternalId");
    }

    [Fact]
    public void Should_Generate_Internal_Builder_When_Specified()
    {
        // Arrange
        var source = """
            using SourceGenerators.DesignPatterns.Builder;

            [GenerateBuilder(Accessibility = BuilderAccessibility.Internal)]
            public class Person
            {
                public string Name { get; set; } = "";
            }
            """;

        var driver = CreateDriver();
        var compilation = CreateCompilation(source);

        // Act
        var result = driver.RunGenerators(compilation).GetRunResult();

        // Assert
        result.Diagnostics.ShouldBeEmpty();

        var builderSource = result.GeneratedTrees
            .First(t => t.FilePath.Contains("PersonBuilder")).ToString();

        builderSource.ShouldContain("internal sealed class PersonBuilder");
    }

    [Fact]
    public void Should_Handle_Constructor_Only_Classes()
    {
        // Arrange
        var source = """
            using SourceGenerators.DesignPatterns.Builder;

            [GenerateBuilder]
            public class ImmutablePerson
            {
                public ImmutablePerson(string name, int age)
                {
                    Name = name;
                    Age = age;
                }

                public string Name { get; }
                public int Age { get; }
            }
            """;

        var driver = CreateDriver();
        var compilation = CreateCompilation(source);

        // Act
        var result = driver.RunGenerators(compilation).GetRunResult();

        // Assert
        result.Diagnostics.ShouldBeEmpty();

        var builderSource = result.GeneratedTrees
            .First(t => t.FilePath.Contains("ImmutablePersonBuilder")).ToString();

        // Should use constructor instead of object initializer
        builderSource.ShouldContain("return new ImmutablePerson(");
        builderSource.ShouldNotContain("return new ImmutablePerson\n            {");
    }

    [Fact]
    public void Should_Not_Generate_Builder_For_Class_Without_Attribute()
    {
        // Arrange
        var source = """
            public class Person
            {
                public string Name { get; set; } = "";
            }
            """;

        var driver = CreateDriver();
        var compilation = CreateCompilation(source);

        // Act
        var result = driver.RunGenerators(compilation).GetRunResult();

        // Assert
        result.Diagnostics.ShouldBeEmpty();
        result.GeneratedTrees.Length.ShouldBe(1); // Only attributes source
        result.GeneratedTrees[0].FilePath.ShouldContain("BuilderAttributes");
    }

    [Fact]
    public void Should_Skip_Validation_When_ValidateOnBuild_Is_False()
    {
        // Arrange
        var source = """
                     using SourceGenerators.DesignPatterns.Builder;

                     [GenerateBuilder(ValidateOnBuild = false)]
                     public class Person
                     {
                         [BuilderProperty(Required = true)]
                         public string Name { get; set; } = "";
                     }
                     """;

        var driver = CreateDriver();
        var compilation = CreateCompilation(source);

        // Act
        var result = driver.RunGenerators(compilation).GetRunResult();

        // Assert
        result.Diagnostics.ShouldBeEmpty();

        var builderSource = result.GeneratedTrees
            .First(t => t.FilePath.Contains("PersonBuilder")).ToString();

        // Validation infrastructure should still exist
        builderSource.ShouldContain("private bool _nameSet = false;");
        builderSource.ShouldContain("_nameSet = true;");
        builderSource.ShouldContain("public bool CanBuild()");
        builderSource.ShouldContain("public IEnumerable<string> GetMissingRequiredProperties()");

        // But Build() method should NOT throw exceptions
        builderSource.ShouldNotContain("throw new InvalidOperationException");
    
        // The validation check should not be in Build method when ValidateOnBuild = false
        var buildMethodStart = builderSource.IndexOf("public Person Build()");
        var buildMethodEnd = builderSource.IndexOf("}", buildMethodStart);
        var buildMethod = builderSource.Substring(buildMethodStart, buildMethodEnd - buildMethodStart);
    
        buildMethod.ShouldNotContain("if (!_nameSet)");
    }
}