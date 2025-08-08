using CodeGenerator.Patterns.Builder;

namespace Demo.Builder.DotNetCore.SampleModels;

[GenerateBuilder]
public record ProjectConfiguration
{
	[BuilderProperty(Required = true)]
	public string Name { get; init; } = "";

	[BuilderProperty]
	public string? Description { get; init; }

	[BuilderCollection(AddMethodName = "AddDependency")]
	public IReadOnlyList<string> Dependencies { get; init; } = Array.Empty<string>();

	[BuilderCollection]
	public List<string> Tags { get; init; } = new();

	[BuilderProperty(ValidatorMethod = nameof(ValidateVersion))]
	public Version Version { get; init; } = new(1, 0, 0);

	[BuilderProperty]
	public Dictionary<string, string> Metadata { get; init; } = new();

	public static bool ValidateVersion(Version version) => version != null && version.Major > 0;
}
