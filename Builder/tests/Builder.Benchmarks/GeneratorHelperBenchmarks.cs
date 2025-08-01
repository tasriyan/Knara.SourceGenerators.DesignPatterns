using BenchmarkDotNet.Attributes;
using System.Text;

namespace Builder.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class GeneratorHelpersBenchmarks
{
    private const int IterationCount = 1000;

    [Benchmark]
    public void CamelCaseConversion()
    {
        var testStrings = new[] { "FirstName", "LastName", "OrderId", "CustomerEmail", "TotalAmount" };
        
        for (int i = 0; i < IterationCount; i++)
        {
            foreach (var str in testStrings)
            {
                _ = CamelCase(str);
            }
        }
    }

    [Benchmark]
    public void StringBuilderAppend()
    {
        var sb = new StringBuilder();
        
        for (int i = 0; i < IterationCount; i++)
        {
            sb.Clear();
            sb.AppendLine("public class TestBuilder");
            sb.AppendLine("{");
            sb.AppendLine("    private string _name = \"\";");
            sb.AppendLine("    public TestBuilder WithName(string name)");
            sb.AppendLine("    {");
            sb.AppendLine("        _name = name;");
            sb.AppendLine("        return this;");
            sb.AppendLine("    }");
            sb.AppendLine("}");
        }
    }

    [Benchmark]
    public void DefaultValueGeneration()
    {
        var properties = new[]
        {
            new { TypeName = "string", AllowNull = false, DefaultValue = "" },
            new { TypeName = "int", AllowNull = false, DefaultValue = "" },
            new { TypeName = "bool", AllowNull = false, DefaultValue = "" },
            new { TypeName = "DateTime", AllowNull = false, DefaultValue = "" },
            new { TypeName = "string?", AllowNull = true, DefaultValue = "" }
        };

        for (int i = 0; i < IterationCount; i++)
        {
            foreach (var prop in properties)
            {
                _ = GetDefaultValueForType(prop.TypeName, prop.AllowNull, prop.DefaultValue);
            }
        }
    }

    [Benchmark]
    public void TypeDetection()
    {
        var typeNames = new[]
        {
            "string",
            "List<string>",
            "Dictionary<string, string>",
            "IReadOnlyList<int>",
            "KeyValuePair<string, string>",
            "int?",
            "DateTime"
        };

        for (int i = 0; i < IterationCount; i++)
        {
            foreach (var typeName in typeNames)
            {
                _ = IsDictionaryType(typeName);
                _ = IsReferenceType(typeName);
            }
        }
    }

    private static string CamelCase(string input) =>
        string.IsNullOrEmpty(input) ? input : char.ToLower(input[0]) + input.Substring(1);

    private static string GetDefaultValueForType(string typeName, bool allowNull, string defaultValue)
    {
        if (!string.IsNullOrEmpty(defaultValue))
            return $" = {defaultValue}";

        if (typeName.EndsWith("?") || allowNull)
            return "";

        return typeName switch
        {
            "string" => " = \"\"",
            "int" => " = 0",
            "bool" => " = false",
            "DateTime" => " = default",
            "TimeSpan" => " = default",
            _ => ""
        };
    }

    private static bool IsDictionaryType(string typeName)
    {
        return typeName.Contains("Dictionary<") || typeName.Contains("IDictionary<");
    }

    private static bool IsReferenceType(string typeName)
    {
        var valueTypes = new[]
        {
            "int", "bool", "double", "float", "decimal", "long", "short", "byte", "sbyte",
            "uint", "ulong", "ushort", "char", "DateTime", "TimeSpan", "Guid",
            "KeyValuePair<", "ValueTuple<"
        };

        return !valueTypes.Any(vt => typeName.Contains(vt)) && !typeName.EndsWith("?");
    }
}