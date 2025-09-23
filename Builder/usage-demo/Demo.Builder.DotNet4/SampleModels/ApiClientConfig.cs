using Knara.SourceGenerators.DesignPatterns.Builder;

namespace Demo.Builder.DotNet4.SampleModels
{
	[GenerateBuilder(BuilderName = "ApiConfigBuilder")]
	public class ApiClientConfig
	{
		[BuilderProperty(Required = true)]
		public string BaseUrl { get; set; } = "";

		[BuilderProperty]
		public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

		[BuilderProperty]
		public AuthenticationType AuthType { get; set; } = AuthenticationType.None;

		[BuilderProperty]
		public string? ApiKey { get; set; }

		[BuilderProperty]
		public string? Username { get; set; }

		[BuilderProperty]
		public string? Password { get; set; }

		[BuilderCollection]
		public IReadOnlyList<string> DefaultHeaders { get; set; } = Array.Empty<string>();

		[BuilderProperty(DefaultValue = "3")]
		public int RetryAttempts { get; set; } = 3;
	}

	public enum AuthenticationType
	{
		None,
		ApiKey,
		BasicAuth,
		OAuth
	}
}