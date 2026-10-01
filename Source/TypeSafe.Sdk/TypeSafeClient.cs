using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TypeSafe.Sdk.Internal;

namespace TypeSafe.Sdk;

/// <summary>
/// 	A reusable, thread-safe HTTP client for the TypeSafe API.
/// </summary>
public sealed class TypeSafeClient : ITypeSafeClient
{
	private const string SystemOnePath = "/v1/systemone";
	private const string RetryCountHeader = "X-TypeSafe-Retry-Count";
	private const int MinimumRedactedLength = 8;

	private readonly HttpClient _httpClient;
	private readonly bool _ownsHttpClient;
	private readonly Uri _endpoint;
	private readonly string _apiKey;
	private readonly string _defaultModel;
	private readonly IReadOnlyDictionary<string, string> _defaultHeaders;
	private readonly int _maxRetries;
	private readonly RetryOptions _retry;
	private readonly TimeSpan _timeout;
	private readonly TimeProvider _time;
	private readonly int _maxResponseBytes;
	private readonly ILogger<TypeSafeClient> _logger;
	private readonly string[] _secrets;
	private int _disposed;

	/// <summary>
	/// 	Creates a client that reads the API key from the <c>TYPESAFE_API_KEY</c> environment variable.
	/// </summary>
	/// <exception cref="TypeSafeException">
	/// 	The environment variable is not set.
	/// </exception>
	public TypeSafeClient() : this(new TypeSafeClientOptions())
	{
	}

	/// <summary>
	/// 	Creates a client and copies its options.
	/// </summary>
	/// <param name="options">
	/// 	Key, endpoint, model, retry, and transport settings.
	/// </param>
	/// <param name="httpClient">
	/// 	An optional HTTP client that the caller owns.
	/// 	Its timeout also applies.
	/// </param>
	/// <param name="logger">
	/// 	An optional logger.
	/// 	The client never logs bodies, headers, or keys.
	/// </param>
	/// <exception cref="ArgumentException">
	/// 	A setting is invalid.
	/// </exception>
	/// <exception cref="TypeSafeException">
	/// 	No API key is set in the options or the environment.
	/// </exception>
	/// <remarks>
	/// 	The client disables redirects on the HTTP client that it creates.
	/// 	Configure a caller-owned HTTP client in the same way.
	/// </remarks>
	public TypeSafeClient(TypeSafeClientOptions options, HttpClient? httpClient = null, ILogger<TypeSafeClient>? logger = null)
	{
		ArgumentNullException.ThrowIfNull(options);
		_apiKey = ResolveApiKey(options);
		_endpoint = ResolveEndpoint(options.BaseUrl ?? ReadUriEnvironmentVariable(TypeSafeClientOptions.BaseUrlEnvironmentVariable) ?? new(TypeSafeClientOptions.DefaultBaseUrl));
		ValidateSettings(options);
		_defaultModel = options.DefaultModel ?? ReadEnvironmentVariable(TypeSafeClientOptions.DefaultModelEnvironmentVariable) ?? TypeSafeModels.Latest;
		RequestSerializer.ValidateModel(_defaultModel);
		_defaultHeaders = CopyHeaders(options.DefaultHeaders);
		_retry = new()
		{
			MaxRetries = options.Retry.MaxRetries,
			InitialBackoff = options.Retry.InitialBackoff,
			MaxBackoff = options.Retry.MaxBackoff,
			Jitter = options.Retry.Jitter,
			RespectRetryAfter = options.Retry.RespectRetryAfter,
			MaxRetryAfter = options.Retry.MaxRetryAfter,
			RetryConnectionErrors = options.Retry.RetryConnectionErrors,
			RetryTimeouts = options.Retry.RetryTimeouts
		};
		_maxRetries = _retry.MaxRetries;
		_timeout = options.Timeout;
		_time = options.TimeProvider;
		_maxResponseBytes = options.MaxResponseBytes;
		_logger = logger ?? NullLogger<TypeSafeClient>.Instance;

		// Short header values (ex. "1" or "true") would mask unrelated text in error details.
		_secrets = _defaultHeaders.Values
			.Where(value => value.Length >= MinimumRedactedLength)
			.Append(_apiKey)
			.Distinct()
			.OrderByDescending(value => value.Length)
			.ToArray();
		_ownsHttpClient = httpClient is null;
		_httpClient = httpClient ?? new HttpClient(new SocketsHttpHandler { AllowAutoRedirect = false, PooledConnectionLifetime = TimeSpan.FromMinutes(5) })
		{
			Timeout = System.Threading.Timeout.InfiniteTimeSpan
		};
	}

	/// <inheritdoc />
	public async Task<SystemOneResponse> SystemOneAsync(SystemOneRequest request, CancellationToken cancellationToken = default)
	{
		ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
		cancellationToken.ThrowIfCancellationRequested();
		PreparedRequest prepared = RequestSerializer.Prepare(request, _defaultModel);
		long started = _time.GetTimestamp();
		for (int attempt = 1; ; attempt++)
		{
			cancellationToken.ThrowIfCancellationRequested();
			TimeSpan? serverDelay = null;
			try
			{
				SystemOneResponse result = await SendAttemptAsync(prepared, attempt, cancellationToken).ConfigureAwait(false);
				_logger.LogInformation(
					"TypeSafe System One request completed in {DurationMs} ms after {Attempts} attempt(s).",
					_time.GetElapsedTime(started).TotalMilliseconds, attempt);
				return result;
			}
			catch (ApiException ex) when (attempt <= _maxRetries && HttpFailureClassifier.IsTransient(ex.StatusCode))
			{
				serverDelay = _retry.RespectRetryAfter && ex.RetryAfter is not null
					? TimeSpan.FromTicks(Math.Min(ex.RetryAfter.Value.Ticks, _retry.MaxRetryAfter.Ticks))
					: null;
			}
			catch (ApiTimeoutException) when (_retry.RetryTimeouts && attempt <= _maxRetries)
			{
				// The attempt timeout is independent of caller cancellation.
			}
			catch (ApiConnectionException ex) when (_retry.RetryConnectionErrors && ex.IsTransient && attempt <= _maxRetries)
			{
				// Only classified connection failures are retried.
			}

			TimeSpan delay = ComputeDelay(attempt, serverDelay);
			_logger.LogWarning(
				"Retrying TypeSafe System One request after attempt {Attempt}; delay {DelayMs} ms.",
				attempt, delay.TotalMilliseconds);
			await DelayAsync(delay, cancellationToken).ConfigureAwait(false);
		}
	}

	/// <summary>
	/// 	Disposes the HTTP client that this instance owns.
	/// 	An HTTP client from the caller stays open.
	/// </summary>
	public void Dispose()
	{
		if (Interlocked.Exchange(ref _disposed, 1) == 0 && _ownsHttpClient)
		{
			_httpClient.Dispose();
		}
	}

	private static string ResolveApiKey(TypeSafeClientOptions options)
	{
		string? key = options.ApiKey?.Trim() ?? ReadEnvironmentVariable(TypeSafeClientOptions.ApiKeyEnvironmentVariable);
		if (string.IsNullOrEmpty(key))
		{
			throw new TypeSafeException($"Set an API key in the client options or in the {TypeSafeClientOptions.ApiKeyEnvironmentVariable} environment variable.");
		}

		if (key.Any(character => character is < '!' or > '~'))
		{
			throw new ArgumentException("The API key must contain only printable ASCII characters without spaces.", nameof(options));
		}

		return key;
	}

	private static string? ReadEnvironmentVariable(string name)
	{
		string? value = Environment.GetEnvironmentVariable(name)?.Trim();
		return string.IsNullOrEmpty(value) ? null : value;
	}

	private static Uri? ReadUriEnvironmentVariable(string name)
	{
		string? value = ReadEnvironmentVariable(name);
		if (value is null)
		{
			return null;
		}

		return Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
			? uri
			: throw new ArgumentException($"The {name} environment variable is not an absolute URL.");
	}

	private static Uri ResolveEndpoint(Uri? baseUrl)
	{
		if (baseUrl is not { IsAbsoluteUri: true } uri || uri.Scheme is not ("https" or "http")
			|| uri.UserInfo.Length != 0 || uri.Fragment.Length != 0 || uri.Query.Length != 0)
		{
			throw new ArgumentException("The base URL must be an absolute HTTP(S) URL without user info, query, or fragment.", nameof(baseUrl));
		}

		return new(uri.AbsoluteUri.TrimEnd('/') + SystemOnePath);
	}

	private static void ValidateSettings(TypeSafeClientOptions options)
	{
		RetryOptions retry = options.Retry;
		bool valid = retry is not null
			&& retry.MaxRetries >= 0
			&& retry.InitialBackoff >= TimeSpan.Zero
			&& retry.MaxBackoff >= retry.InitialBackoff
			&& retry.MaxBackoff.TotalMilliseconds < uint.MaxValue
			&& retry.Jitter is >= 0 and <= 1
			&& retry.MaxRetryAfter >= TimeSpan.Zero
			&& options.Timeout > TimeSpan.Zero
			&& options.Timeout.TotalMilliseconds < uint.MaxValue
			&& options.TimeProvider is not null
			&& options.MaxResponseBytes >= 1;
		if (valid is false)
		{
			throw new ArgumentException("Retry, timeout, time provider, or response size settings are invalid.", nameof(options));
		}
	}

	private static Dictionary<string, string> CopyHeaders(IDictionary<string, string> headers)
	{
		Dictionary<string, string> copy = new(StringComparer.OrdinalIgnoreCase);
		using (HttpRequestMessage probe = new())
		{
			foreach (KeyValuePair<string, string> pair in headers)
			{
				bool reserved = string.Equals(pair.Key, "Authorization", StringComparison.OrdinalIgnoreCase)
					|| string.Equals(pair.Key, RetryCountHeader, StringComparison.OrdinalIgnoreCase);
				if (reserved || pair.Value is not { } value || value.Any(char.IsControl)
					|| probe.Headers.TryAddWithoutValidation(pair.Key, value) is false)
				{
					throw new ArgumentException("A default header is invalid or reserved.", nameof(headers));
				}

				copy[pair.Key] = value;
			}
		}

		return copy;
	}

	private TimeSpan ComputeDelay(int attempt, TimeSpan? serverDelay)
	{
		double milliseconds = Math.Min(_retry.MaxBackoff.TotalMilliseconds, _retry.InitialBackoff.TotalMilliseconds * Math.Pow(2, attempt - 1));
		milliseconds *= 1 - (_retry.Jitter * Random.Shared.NextDouble());
		TimeSpan delay = TimeSpan.FromMilliseconds(milliseconds);
		return serverDelay > delay ? serverDelay.Value : delay;
	}

	/// <summary>
	/// 	Runs one bounded HTTP attempt and releases all resources before a retry wait.
	/// </summary>
	private async Task<SystemOneResponse> SendAttemptAsync(PreparedRequest prepared, int attempt, CancellationToken cancellationToken)
	{
		using (CancellationTokenSource expiration = new(_timeout, _time))
		{
			using (CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, expiration.Token))
			{
				try
				{
					using (HttpRequestMessage message = CreateRequest(prepared, attempt))
					{
						using (HttpResponseMessage response = await _httpClient.SendAsync(
							message, HttpCompletionOption.ResponseHeadersRead, linked.Token).ConfigureAwait(false))
						{
							string? requestId = Redact(GetRequestId(response));
							if (response.IsSuccessStatusCode is false)
							{
								string? details = await ReadErrorDetailsAsync(response, linked.Token, cancellationToken).ConfigureAwait(false);
								cancellationToken.ThrowIfCancellationRequested();
								throw CreateApiException(response.StatusCode, attempt, requestId, details, GetRetryAfter(response));
							}

							byte[]? bytes = await ReadBodyAsync(response, _maxResponseBytes, linked.Token).ConfigureAwait(false);
							cancellationToken.ThrowIfCancellationRequested();
							if (bytes is null)
							{
								throw new InvalidResponseException(attempt, requestId);
							}

							return ResponseParser.Parse(bytes, prepared, attempt, requestId);
						}
					}
				}
				catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
				{
					throw new OperationCanceledException(cancellationToken);
				}
				catch (OperationCanceledException)
				{
					throw new ApiTimeoutException(attempt);
				}
				catch (HttpRequestException ex)
				{
					throw new ApiConnectionException(attempt, HttpFailureClassifier.IsTransient(ex));
				}
				catch (IOException)
				{
					throw new ApiConnectionException(attempt, true);
				}
			}
		}
	}

	/// <summary>
	/// 	Creates an isolated message whose body is identical across attempts.
	/// </summary>
	private HttpRequestMessage CreateRequest(PreparedRequest prepared, int attempt)
	{
		HttpRequestMessage message = new(HttpMethod.Post, _endpoint)
		{
			Content = new ByteArrayContent(prepared.Body)
		};
		message.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		message.Headers.Accept.Add(new("application/json"));
		message.Headers.UserAgent.ParseAdd("typesafe-sdk-csharp/" + typeof(TypeSafeClient).Assembly.GetName().Version);
		foreach (KeyValuePair<string, string> header in _defaultHeaders)
		{
			message.Headers.TryAddWithoutValidation(header.Key, header.Value);
		}

		message.Headers.Authorization = new("Bearer", _apiKey);
		message.Headers.TryAddWithoutValidation(RetryCountHeader, (attempt - 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
		return message;
	}

	/// <summary>
	/// 	Reads optional error details and keeps a known HTTP failure when the body cannot be read.
	/// </summary>
	private async Task<string?> ReadErrorDetailsAsync(HttpResponseMessage response, CancellationToken attemptToken, CancellationToken callerToken)
	{
		try
		{
			byte[]? bytes = await ReadBodyAsync(response, 64 * 1024, attemptToken).ConfigureAwait(false);
			return bytes is null ? null : Redact(Encoding.UTF8.GetString(bytes));
		}
		catch (OperationCanceledException) when (callerToken.IsCancellationRequested is false)
		{
			return null;
		}
		catch (Exception ex) when (ex is HttpRequestException or IOException)
		{
			return null;
		}
	}

	/// <summary>
	/// 	Reads a bounded body.
	/// 	Returns null when the body exceeds the limit.
	/// </summary>
	private static async Task<byte[]?> ReadBodyAsync(HttpResponseMessage response, int limit, CancellationToken cancellationToken)
	{
		if (response.Content.Headers.ContentLength > limit)
		{
			return null;
		}

		using (Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
		{
			using (MemoryStream output = new())
			{
				byte[] buffer = new byte[8192];
				int read;
				while ((read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) != 0)
				{
					if (output.Length + read > limit)
					{
						return null;
					}

					output.Write(buffer, 0, read);
				}

				return output.ToArray();
			}
		}
	}

	/// <summary>
	/// 	Reads <c>retry-after-ms</c> first, then <c>Retry-After</c> as seconds or an HTTP date.
	/// </summary>
	private TimeSpan? GetRetryAfter(HttpResponseMessage response)
	{
		if (response.Headers.TryGetValues("retry-after-ms", out IEnumerable<string>? values)
			&& double.TryParse(values.FirstOrDefault(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double milliseconds)
			&& double.IsFinite(milliseconds) && milliseconds >= 0 && milliseconds < TimeSpan.MaxValue.TotalMilliseconds)
		{
			return TimeSpan.FromMilliseconds(milliseconds);
		}

		RetryConditionHeaderValue? header = response.Headers.RetryAfter;
		TimeSpan? after = header?.Delta ?? (header?.Date - _time.GetUtcNow());
		return after >= TimeSpan.Zero ? after : null;
	}

	/// <summary>
	/// 	Waits in bounded chunks so that long delays stay valid and cancellation is immediate.
	/// </summary>
	private async Task DelayAsync(TimeSpan duration, CancellationToken cancellationToken)
	{
		TimeSpan maximum = TimeSpan.FromDays(1);
		while (duration > maximum)
		{
			await Task.Delay(maximum, _time, cancellationToken).ConfigureAwait(false);
			duration -= maximum;
		}

		await Task.Delay(duration, _time, cancellationToken).ConfigureAwait(false);
	}

	private static string? GetRequestId(HttpResponseMessage response)
	{
		foreach (string name in new[] { "x-typesafe-request-id", "x-request-id", "request-id" })
		{
			if (response.Headers.TryGetValues(name, out IEnumerable<string>? values))
			{
				return values.FirstOrDefault();
			}
		}

		return null;
	}

	/// <summary>
	/// 	Removes the API key and default header values from error details and identifiers.
	/// </summary>
	private string? Redact(string? value)
	{
		if (value is null)
		{
			return null;
		}

		foreach (string secret in _secrets)
		{
			value = value.Replace(secret, "[REDACTED]", StringComparison.Ordinal);
			value = value.Replace(JsonSerializer.Serialize(secret)[1..^1], "[REDACTED]", StringComparison.Ordinal);
		}

		return value;
	}

	/// <summary>
	/// 	Maps HTTP errors to documented exception types without exposing details in messages.
	/// </summary>
	private static ApiException CreateApiException(HttpStatusCode status, int attempt, string? requestId, string? details, TimeSpan? retryAfter)
	{
		return status switch
		{
			HttpStatusCode.Unauthorized => new ApiAuthenticationException(attempt, requestId, details),
			HttpStatusCode.Forbidden => new ApiPermissionException(attempt, requestId, details),
			HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => new ApiValidationException(attempt, status, requestId, details),
			HttpStatusCode.TooManyRequests => new ApiRateLimitException(attempt, requestId, details, retryAfter),
			_ => new ApiException(attempt, status, requestId, details, retryAfter)
		};
	}
}
