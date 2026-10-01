namespace TypeSafe.Sdk;

/// <summary>
/// 	Configures a <see cref="TypeSafeClient" />.
/// 	The client copies the values when it is constructed.
/// </summary>
/// <remarks>
/// 	For the API key, base URL, and default model, the client uses the option value first.
/// 	When the option is null, it uses the environment variable.
/// 	When the environment variable is not set, it uses the default.
/// </remarks>
public sealed class TypeSafeClientOptions
{
	/// <summary>
	/// 	The environment variable that supplies the API key when <see cref="ApiKey" /> is null.
	/// </summary>
	public const string ApiKeyEnvironmentVariable = "TYPESAFE_API_KEY";

	/// <summary>
	/// 	The environment variable that supplies the API root when <see cref="BaseUrl" /> is null.
	/// </summary>
	public const string BaseUrlEnvironmentVariable = "TYPESAFE_BASE_URL";

	/// <summary>
	/// 	The environment variable that supplies the default model when <see cref="DefaultModel" /> is null.
	/// </summary>
	public const string DefaultModelEnvironmentVariable = "TYPESAFE_DEFAULT_MODEL";

	/// <summary>
	/// 	The default API root.
	/// </summary>
	public const string DefaultBaseUrl = "https://api.typesafe.ai";

	/// <summary>
	/// 	Gets or sets the API key.
	/// 	Null reads the <c>TYPESAFE_API_KEY</c> environment variable.
	/// </summary>
	public string? ApiKey { get; set; }

	/// <summary>
	/// 	Gets or sets the API root.
	/// 	Null reads the <c>TYPESAFE_BASE_URL</c> environment variable, then uses <see cref="DefaultBaseUrl" />.
	/// 	A trailing slash is optional.
	/// </summary>
	public Uri? BaseUrl { get; set; }

	/// <summary>
	/// 	Gets or sets the model that requests use when they set none.
	/// 	Null reads the <c>TYPESAFE_DEFAULT_MODEL</c> environment variable, then uses <see cref="TypeSafeModels.Latest" />.
	/// </summary>
	public string? DefaultModel { get; set; }

	/// <summary>
	/// 	Gets or sets the timeout of each attempt, including the read of the response.
	/// </summary>
	public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

	/// <summary>
	/// 	Gets the retry configuration.
	/// </summary>
	public RetryOptions Retry { get; init; } = new();

	/// <summary>
	/// 	Gets headers that the client adds to every request.
	/// 	The collection cannot hold the Authorization header or control characters.
	/// </summary>
	public IDictionary<string, string> DefaultHeaders { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// 	Gets or sets the largest accepted success response, in bytes.
	/// </summary>
	public int MaxResponseBytes { get; set; } = 8 * 1024 * 1024;

	/// <summary>
	/// 	Gets or sets the time source for timeouts and delays.
	/// </summary>
	public TimeProvider TimeProvider { get; set; } = TimeProvider.System;
}
