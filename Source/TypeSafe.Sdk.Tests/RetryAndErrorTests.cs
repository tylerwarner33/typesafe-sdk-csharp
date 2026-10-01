using System.Net;
using Microsoft.Extensions.Time.Testing;
using TypeSafe.Sdk.Tests.Support;

namespace TypeSafe.Sdk.Tests;

public sealed class RetryAndErrorTests
{
	[Fact]
	public async Task Retries_transient_status_codes_and_reports_the_retry_count()
	{
		StubHandler handler = new((attempt, _) => Task.FromResult(attempt < 3
			? StubHandler.Json("{}", HttpStatusCode.ServiceUnavailable)
			: StubHandler.Json(Fixtures.Single)));
		using (TypeSafeClient client = Fixtures.Client(handler))
		{
			SystemOneResponse response = await client.SystemOneAsync(Fixtures.One());
			Assert.Equal(0.9, response.Nouls["yes"].Noul);
		}

		Assert.Equal(3, handler.Requests.Count);
		Assert.Equal(new[] { "0", "1", "2" }, handler.Requests.Select(request => request.Headers["X-TypeSafe-Retry-Count"]));
		Assert.Single(handler.Requests.Select(request => request.Body).Distinct());
	}

	[Fact]
	public async Task Stops_after_the_configured_retries()
	{
		StubHandler handler = new("{}", HttpStatusCode.BadGateway);
		using (TypeSafeClient client = Fixtures.Client(handler, Fixtures.Options(1)))
		{
			ApiException exception = await Assert.ThrowsAsync<ApiException>(() => client.SystemOneAsync(Fixtures.One()));
			Assert.Equal(HttpStatusCode.BadGateway, exception.StatusCode);
			Assert.Equal(2, exception.Attempts);
		}

		Assert.Equal(2, handler.Requests.Count);
	}

	[Fact]
	public async Task Zero_retries_sends_one_attempt()
	{
		StubHandler handler = new("{}", HttpStatusCode.InternalServerError);
		using (TypeSafeClient client = Fixtures.Client(handler, Fixtures.Options(0)))
		{
			await Assert.ThrowsAsync<ApiException>(() => client.SystemOneAsync(Fixtures.One()));
		}

		Assert.Single(handler.Requests);
	}

	[Theory]
	[InlineData(HttpStatusCode.Unauthorized, typeof(ApiAuthenticationException))]
	[InlineData(HttpStatusCode.Forbidden, typeof(ApiPermissionException))]
	[InlineData(HttpStatusCode.BadRequest, typeof(ApiValidationException))]
	[InlineData(HttpStatusCode.UnprocessableEntity, typeof(ApiValidationException))]
	[InlineData(HttpStatusCode.NotFound, typeof(ApiException))]
	public async Task Maps_permanent_failures_without_retrying(HttpStatusCode status, Type expected)
	{
		StubHandler handler = new("""{"error":"nope"}""", status);
		using (TypeSafeClient client = Fixtures.Client(handler))
		{
			Exception thrown = await Assert.ThrowsAsync(expected, () => client.SystemOneAsync(Fixtures.One()));
			ApiException exception = Assert.IsAssignableFrom<ApiException>(thrown);
			Assert.Equal(status, exception.StatusCode);
			Assert.Equal("request-42", exception.RequestId);
			Assert.Equal("""{"error":"nope"}""", exception.Details);
			Assert.DoesNotContain("nope", exception.Message);
		}

		Assert.Single(handler.Requests);
	}

	[Fact]
	public async Task Redacts_the_api_key_from_error_details()
	{
		StubHandler handler = new($$"""{"echo":"{{Fixtures.ApiKey}}"}""", HttpStatusCode.BadRequest);
		using (TypeSafeClient client = Fixtures.Client(handler))
		{
			ApiException exception = await Assert.ThrowsAsync<ApiValidationException>(() => client.SystemOneAsync(Fixtures.One()));
			Assert.DoesNotContain(Fixtures.ApiKey, exception.Details);
			Assert.Contains("[REDACTED]", exception.Details);
		}
	}

	[Fact]
	public async Task Backoff_doubles_from_the_initial_delay()
	{
		FakeTimeProvider time = new();
		StubHandler handler = new((attempt, _) => Task.FromResult(attempt < 3
			? StubHandler.Json("{}", HttpStatusCode.ServiceUnavailable)
			: StubHandler.Json(Fixtures.Single)));
		TypeSafeClientOptions options = Fixtures.Options();
		options.TimeProvider = time;
		options.Retry.InitialBackoff = TimeSpan.FromMilliseconds(500);
		options.Retry.MaxBackoff = TimeSpan.FromSeconds(5);
		using (TypeSafeClient client = Fixtures.Client(handler, options))
		{
			Task<SystemOneResponse> pending = client.SystemOneAsync(Fixtures.One());
			await Fixtures.EventuallyAsync(() => handler.Requests.Count == 1);

			time.Advance(TimeSpan.FromMilliseconds(499));
			await Fixtures.SettleAsync();
			Assert.Single(handler.Requests);
			time.Advance(TimeSpan.FromMilliseconds(1));
			await Fixtures.EventuallyAsync(() => handler.Requests.Count == 2);

			time.Advance(TimeSpan.FromMilliseconds(999));
			await Fixtures.SettleAsync();
			Assert.Equal(2, handler.Requests.Count);
			time.Advance(TimeSpan.FromMilliseconds(1));
			await pending;
		}

		Assert.Equal(3, handler.Requests.Count);
	}

	[Fact]
	public async Task Caps_a_long_retry_after_at_the_maximum()
	{
		FakeTimeProvider time = new();
		StubHandler handler = new((attempt, _) =>
		{
			HttpResponseMessage response = attempt == 1 ? StubHandler.Json("{}", HttpStatusCode.TooManyRequests) : StubHandler.Json(Fixtures.Single);
			response.Headers.RetryAfter = new(TimeSpan.FromHours(1));
			return Task.FromResult(response);
		});
		TypeSafeClientOptions options = Fixtures.Options();
		options.TimeProvider = time;
		using (TypeSafeClient client = Fixtures.Client(handler, options))
		{
			Task<SystemOneResponse> pending = client.SystemOneAsync(Fixtures.One());
			await Fixtures.EventuallyAsync(() => handler.Requests.Count == 1);
			time.Advance(TimeSpan.FromSeconds(59));
			await Fixtures.SettleAsync();
			Assert.Single(handler.Requests);
			time.Advance(TimeSpan.FromSeconds(1));
			await pending;
		}

		Assert.Equal(2, handler.Requests.Count);
	}

	[Fact]
	public async Task Prefers_retry_after_ms_over_retry_after()
	{
		FakeTimeProvider time = new();
		StubHandler handler = new((attempt, _) =>
		{
			HttpResponseMessage response = attempt == 1 ? StubHandler.Json("{}", HttpStatusCode.ServiceUnavailable) : StubHandler.Json(Fixtures.Single);
			response.Headers.RetryAfter = new(TimeSpan.FromSeconds(30));
			response.Headers.Add("retry-after-ms", "1500");
			return Task.FromResult(response);
		});
		TypeSafeClientOptions options = Fixtures.Options();
		options.TimeProvider = time;
		using (TypeSafeClient client = Fixtures.Client(handler, options))
		{
			Task<SystemOneResponse> pending = client.SystemOneAsync(Fixtures.One());
			await Fixtures.EventuallyAsync(() => handler.Requests.Count == 1);
			time.Advance(TimeSpan.FromMilliseconds(1500));
			await pending;
		}

		Assert.Equal(2, handler.Requests.Count);
	}

	[Fact]
	public async Task Ignores_server_delays_when_retry_after_is_disabled()
	{
		StubHandler handler = new((attempt, _) =>
		{
			HttpResponseMessage response = attempt == 1 ? StubHandler.Json("{}", HttpStatusCode.TooManyRequests) : StubHandler.Json(Fixtures.Single);
			response.Headers.RetryAfter = new(TimeSpan.FromHours(1));
			return Task.FromResult(response);
		});
		TypeSafeClientOptions options = Fixtures.Options();
		options.Retry.RespectRetryAfter = false;
		using (TypeSafeClient client = Fixtures.Client(handler, options))
		{
			await client.SystemOneAsync(Fixtures.One());
		}

		Assert.Equal(2, handler.Requests.Count);
	}

	[Theory]
	[InlineData(HttpStatusCode.RequestTimeout)]
	[InlineData(HttpStatusCode.NotImplemented)]
	[InlineData(HttpStatusCode.HttpVersionNotSupported)]
	[InlineData((HttpStatusCode)599)]
	public async Task Retries_408_and_every_5xx_status(HttpStatusCode status)
	{
		StubHandler handler = new("{}", status);
		using (TypeSafeClient client = Fixtures.Client(handler))
		{
			await Assert.ThrowsAnyAsync<ApiException>(() => client.SystemOneAsync(Fixtures.One()));
		}

		Assert.Equal(3, handler.Requests.Count);
	}

	[Fact]
	public async Task Reads_the_typesafe_request_id_header_first()
	{
		StubHandler handler = new((_, _) =>
		{
			HttpResponseMessage response = StubHandler.Json(Fixtures.Single);
			response.Headers.Add("x-typesafe-request-id", "typesafe-7");
			return Task.FromResult(response);
		});
		using (TypeSafeClient client = Fixtures.Client(handler))
		{
			SystemOneResponse response = await client.SystemOneAsync(Fixtures.One());
			Assert.Equal("typesafe-7", response.RequestId);
		}
	}

	[Fact]
	public async Task Retries_connection_failures_then_reports_them()
	{
		StubHandler handler = new((_, _) => throw new HttpRequestException(HttpRequestError.ConnectionError, "down"));
		using (TypeSafeClient client = Fixtures.Client(handler))
		{
			ApiConnectionException exception = await Assert.ThrowsAsync<ApiConnectionException>(() => client.SystemOneAsync(Fixtures.One()));
			Assert.True(exception.IsTransient);
			Assert.Equal(3, exception.Attempts);
		}

		Assert.Equal(3, handler.Requests.Count);
	}

	[Fact]
	public async Task Reports_timeouts_after_retrying()
	{
		FakeTimeProvider time = new();
		StubHandler handler = new(async (_, token) =>
		{
			await Task.Delay(Timeout.Infinite, token);
			return StubHandler.Json(Fixtures.Single);
		});
		TypeSafeClientOptions options = Fixtures.Options(1);
		options.TimeProvider = time;
		using (TypeSafeClient client = Fixtures.Client(handler, options))
		{
			Task<SystemOneResponse> pending = client.SystemOneAsync(Fixtures.One());
			await Fixtures.EventuallyAsync(() => handler.Requests.Count == 1);
			time.Advance(options.Timeout);
			await Fixtures.EventuallyAsync(() => handler.Requests.Count == 2);
			time.Advance(options.Timeout);
			ApiTimeoutException exception = await Assert.ThrowsAsync<ApiTimeoutException>(() => pending);
			Assert.Equal(2, exception.Attempts);
		}
	}

	[Fact]
	public async Task Retry_flags_can_disable_connection_and_timeout_retries()
	{
		StubHandler handler = new((_, _) => throw new HttpRequestException(HttpRequestError.ConnectionError, "down"));
		TypeSafeClientOptions options = Fixtures.Options();
		options.Retry.RetryConnectionErrors = false;
		using (TypeSafeClient client = Fixtures.Client(handler, options))
		{
			await Assert.ThrowsAsync<ApiConnectionException>(() => client.SystemOneAsync(Fixtures.One()));
		}

		Assert.Single(handler.Requests);
	}

	[Fact]
	public async Task Caller_cancellation_stops_immediately()
	{
		using (CancellationTokenSource cancellation = new())
		{
			StubHandler handler = new((_, _) =>
			{
				cancellation.Cancel();
				return Task.FromResult(StubHandler.Json("{}", HttpStatusCode.ServiceUnavailable));
			});
			using (TypeSafeClient client = Fixtures.Client(handler))
			{
				await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.SystemOneAsync(Fixtures.One(), cancellation.Token));
			}

			Assert.Single(handler.Requests);
		}
	}
}
