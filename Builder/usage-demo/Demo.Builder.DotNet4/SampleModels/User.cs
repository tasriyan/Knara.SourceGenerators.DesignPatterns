using Knara.SourceGenerators.DesignPatterns.Builder;

namespace Demo.Builder.DotNet4.SampleModels
{
	[GenerateBuilder(ValidateOnBuild = true, BuilderName = "UserBuilder", GenerateWithMethods = true)]
	public record User
	{
		[BuilderProperty(Required = true)]
		public string Email { get; set; }

		[BuilderProperty(Required = true)]
		public string FirstName { get; set; } 

		[BuilderProperty(Required = true)]
		public string? LastName { get; set; }

		[BuilderProperty(DefaultValue = "false")]
		public bool IsActive { get; set; } 

		[BuilderProperty(ValidatorMethod = nameof(ValidateAge))]
		public int Age { get; set; }

		[BuilderProperty(IgnoreInBuilder = true)]
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		public static bool ValidateAge(int age) { return age >= 0 && age <= 150; }
	}
}
