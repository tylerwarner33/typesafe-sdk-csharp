using TypeSafe.Sdk.Tests.Support;

namespace TypeSafe.Sdk.Tests;

[Collection("Environment")]
public sealed class ClientConfigurationTests
{
	[Fact]
	public void Reads_the_api_key_from_the_environment()
	{
		string? original = Environment.GetEnvironmentVariable(TypeSafeClientOptions.ApiKeyEnvironmentVariable);
		try
		{
			Environment.SetEnvironmentVariable(TypeSafeClientOptions.ApiKeyEnvironmentVariable, "from-environment");
			using (TypeSafeClient client = new())
			{
				Assert.NotNull(client);
			}

			Environment.SetEnvironmentVariable(TypeSafeClientOptions.ApiKeyEnvironmentVariable, null);
			Assert.Throws<TypeSafeException>(() => new TypeSafeClient());
		}
		finally
		{
			Environment.SetEnvironmentVariable(TypeSafeClientOptions.ApiKeyEnvironmentVariable, original);
		}
	}

	[Fact]
	public async Task Reads_the_base_url_and_default_model_from_the_environment()
	{
		string? originalUrl = Environment.GetEnvironmentVariable(TypeSafeClientOptions.BaseUrlEnvironmentVariable);
		string? originalModel = Environment.GetEnvironmentVariable(TypeSafeClientOptions.DefaultModelEnvironmentVariable);
		try
		{
			Environment.SetEnvironmentVariable(TypeSafeClientOptions.BaseUrlEnvironmentVariable, "https://proxy.example.test/");
			Environment.SetEnvironmentVariable(TypeSafeClientOptions.DefaultModelEnvironmentVariable, "env-model");
			StubHandler handler = new(Fixtures.Single);
			using (TypeSafeClient client = new(new TypeSafeClientOptions { ApiKey = Fixtures.ApiKey }, new HttpClient(handler, false)))
			{
				await client.SystemOneAsync(Fixtures.One());
			}

			CapturedRequest captured = Assert.Single(handler.Requests);
			Assert.Equal("https://proxy.example.test/v1/systemone", captured.Uri.AbsoluteUri);
			Assert.Contains("\"model\":\"env-model\"", captured.Body);

			StubHandler explicitHandler = new(Fixtures.Single);
			using (TypeSafeClient client = Fixtures.Client(explicitHandler))
			{
				await client.SystemOneAsync(Fixtures.One());
			}

			Assert.Equal("https://api.typesafe.ai/v1/systemone", Assert.Single(explicitHandler.Requests).Uri.AbsoluteUri);

			Environment.SetEnvironmentVariable(TypeSafeClientOptions.BaseUrlEnvironmentVariable, "not a url");
			Assert.Throws<ArgumentException>(() => new TypeSafeClient(new TypeSafeClientOptions { ApiKey = Fixtures.ApiKey }));
		}
		finally
		{
			Environment.SetEnvironmentVariable(TypeSafeClientOptions.BaseUrlEnvironmentVariable, originalUrl);
			Environment.SetEnvironmentVariable(TypeSafeClientOptions.DefaultModelEnvironmentVariable, originalModel);
		}
	}

	[Fact]
	public void Rejects_invalid_options()
	{
		Assert.Throws<ArgumentNullException>(() => new TypeSafeClient(null!));
		Assert.Throws<ArgumentException>(() => new TypeSafeClient(Configured(options => options.Timeout = TimeSpan.Zero)));
		Assert.Throws<ArgumentException>(() => new TypeSafeClient(Configured(options => options.Retry.MaxRetries = -1)));
		Assert.Throws<ArgumentException>(() => new TypeSafeClient(Configured(options => options.Retry.Jitter = 2)));
		Assert.Throws<ArgumentException>(() => new TypeSafeClient(Configured(options => options.DefaultModel = " ")));
		Assert.Throws<ArgumentException>(() => new TypeSafeClient(Configured(options => options.BaseUrl = new("/relative", UriKind.Relative))));
		Assert.Throws<ArgumentException>(() => new TypeSafeClient(Configured(options => options.BaseUrl = new("ftp://example.test"))));
		Assert.Throws<ArgumentException>(() => new TypeSafeClient(Configured(options => options.DefaultHeaders["Authorization"] = "Bearer other")));
		Assert.Throws<ArgumentException>(() => new TypeSafeClient(Configured(options => options.DefaultHeaders["X-TypeSafe-Retry-Count"] = "5")));
		Assert.Throws<ArgumentException>(() => new TypeSafeClient(Configured(options => options.DefaultHeaders["X-Team"] = "a\r\nInjected: 1")));
		Assert.Throws<ArgumentException>(() => new TypeSafeClient(Configured(options => options.ApiKey = "bad\nkey")));
		Assert.Throws<ArgumentException>(() => new TypeSafeClient(Configured(options => options.ApiKey = "has space")));
		Assert.Throws<TypeSafeException>(() => new TypeSafeClient(Configured(options => options.ApiKey = "   ")));
	}

	[Fact]
	public async Task Trims_the_api_key()
	{
		StubHandler handler = new(Fixtures.Single);
		using (TypeSafeClient client = Fixtures.Client(handler, Configured(options => options.ApiKey = "  " + Fixtures.ApiKey + "\n")))
		{
			await client.SystemOneAsync(Fixtures.One());
		}

		Assert.Equal("Bearer " + Fixtures.ApiKey, Assert.Single(handler.Requests).Headers["Authorization"]);
	}

	[Fact]
	public void Matches_the_official_sdk_defaults()
	{
		TypeSafeClientOptions options = new();
		Assert.Equal(TimeSpan.FromSeconds(10), options.Timeout);
		Assert.Equal(2, options.Retry.MaxRetries);
		Assert.Equal(TimeSpan.FromMilliseconds(500), options.Retry.InitialBackoff);
		Assert.Equal(TimeSpan.FromSeconds(5), options.Retry.MaxBackoff);
		Assert.Equal(0.25, options.Retry.Jitter);
		Assert.True(options.Retry.RespectRetryAfter);
		Assert.Equal(TimeSpan.FromSeconds(60), options.Retry.MaxRetryAfter);
		Assert.True(options.Retry.RetryConnectionErrors);
		Assert.True(options.Retry.RetryTimeouts);
	}

	private static TypeSafeClientOptions Configured(Action<TypeSafeClientOptions> configure)
	{
		TypeSafeClientOptions options = Fixtures.Options();
		configure(options);
		return options;
	}

	[Fact]
	public async Task Disposal_respects_http_client_ownership()
	{
		StubHandler callerHandler = new(Fixtures.Single);
		using (HttpClient http = new(callerHandler, false))
		{
			TypeSafeClient shared = new(Fixtures.Options(), http);
			shared.Dispose();
			Assert.False(callerHandler.Disposed);
			await Assert.ThrowsAsync<ObjectDisposedException>(() => shared.SystemOneAsync(Fixtures.One()));
		}
	}

	[Fact]
	public async Task A_client_is_safe_for_concurrent_requests()
	{
		StubHandler handler = new(Fixtures.Single);
		using (TypeSafeClient client = Fixtures.Client(handler))
		{
			SystemOneResponse[] responses = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => client.SystemOneAsync(Fixtures.One())));
			Assert.All(responses, response => Assert.Equal(0.9, response.Nouls["yes"].Noul));
		}

		Assert.Equal(16, handler.Requests.Count);
	}
}
