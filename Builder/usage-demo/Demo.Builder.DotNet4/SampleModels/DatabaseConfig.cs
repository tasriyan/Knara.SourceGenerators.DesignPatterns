using CodeGenerator.Patterns.Builder;

namespace Demo.Builder.DotNet4.SampleModels
{
	[GenerateBuilder(ValidateOnBuild = true, GenerateFromMethod = true)]
	public class DatabaseConfig
	{
		[BuilderProperty(Required = true, AllowNull = false)]
		public string ConnectionString { get; }

		[BuilderProperty(ValidatorMethod = nameof(ValidateTimeout))]
		public TimeSpan CommandTimeout { get; }

		[BuilderProperty(CustomSetterName = "WithPoolSize")]
		public int MaxPoolSize { get; }

		[BuilderProperty]
		public bool EnableRetry { get; }

		[BuilderCollection(AddMethodName = "AddTag")]
		public IReadOnlyList<string> Tags { get; }

		public DatabaseConfig(string connectionString,
			TimeSpan? commandTimeout = null,
			int maxPoolSize = 100, 
			bool enableRetry = true,
			List<string>? tags = null)
		{
			ConnectionString = connectionString;
			CommandTimeout = commandTimeout ?? TimeSpan.FromSeconds(30);
			MaxPoolSize = maxPoolSize;
			EnableRetry = enableRetry;
			Tags = tags ?? [];
		}

		public static bool ValidateTimeout(TimeSpan timeout) => timeout > TimeSpan.Zero && timeout <= TimeSpan.FromMinutes(10);
	}
}
