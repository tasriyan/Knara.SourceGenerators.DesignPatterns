using Knara.SourceGenerators.DesignPatters.Builder;

namespace Demo.Builder.DotNetCore.SampleModels;

[GenerateBuilder(BuilderName = "ApiConfigBuilder")]
public class ApiClientConfig
{
	[BuilderProperty(Required = true)]
	public string BaseUrl { get; init; } = "";

	[BuilderProperty]
	public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

	[BuilderProperty]
	public AuthenticationType AuthType { get; init; } = AuthenticationType.None;

	[BuilderProperty]
	public string? ApiKey { get; init; }

	[BuilderProperty]
	public string? Username { get; init; }

	[BuilderProperty]
	public string? Password { get; init; }

	[BuilderCollection]
	public IReadOnlyList<string> DefaultHeaders { get; init; } = Array.Empty<string>();

	[BuilderProperty(DefaultValue = "3")]
	public int RetryAttempts { get; init; } = 3;
}

public enum AuthenticationType
{
	None,
	ApiKey,
	BasicAuth,
	OAuth
}