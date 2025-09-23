using Knara.SourceGenerators.DesignPatterns.Builder;

namespace Demo.Builder.DotNet4.SampleModels
{
	[GenerateBuilder]
	public record ProjectConfiguration
	{
		[BuilderProperty(Required = true)]
		public string Name { get; set; } = "";

		[BuilderProperty]
		public string? Description { get; set; }

		[BuilderCollection(AddMethodName = "AddDependency")]
		public IReadOnlyList<string> Dependencies { get; set; }

		[BuilderCollection]
		public List<string> Tags { get; set; }

		[BuilderProperty(ValidatorMethod = nameof(ValidateVersion))]
		public Version Version { get; set; } = new(1, 0, 0);

		[BuilderProperty]
		public Dictionary<string, string> Metadata { get; set; } 

		public static bool ValidateVersion(Version version)
		{
			return version != null && version.Major > 0;
		}
	}
}
