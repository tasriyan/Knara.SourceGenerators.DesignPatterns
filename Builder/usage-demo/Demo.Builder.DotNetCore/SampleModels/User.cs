using CodeGenerator.Patterns.Builder;

namespace Demo.Builder.DotNetCore.SampleModels;

[GenerateBuilder(ValidateOnBuild = true, BuilderName = "UserBuilder", GenerateWithMethods = true)]
public record User
{
	[BuilderProperty]
	public string Email { get; init; } = "";

	[BuilderProperty(Required = true)]
	public string FirstName { get; init; } = "";

	[BuilderProperty]
	public string? LastName { get; init; }

	[BuilderProperty(DefaultValue = "false")]
	public bool IsActive { get; init; } = false;

	[BuilderProperty(ValidatorMethod = nameof(ValidateAge))]
	public int Age { get; init; }

	[BuilderProperty(IgnoreInBuilder = true)]
	public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

	public static bool ValidateAge(int age) => age >= 0 && age <= 150;
}
