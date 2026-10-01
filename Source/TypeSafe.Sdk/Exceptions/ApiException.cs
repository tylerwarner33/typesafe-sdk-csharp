using System.Net;

namespace TypeSafe.Sdk;

/// <summary>
/// 	An unsuccessful HTTP response from the API.
/// </summary>
public class ApiException : TypeSafeException
{
	/// <summary>
	/// 	Initializes an HTTP failure.
	/// 	The message excludes the response details.
	/// </summary>
	/// <param name="attempts">
	/// 	The number of attempts made.
	/// </param>
	/// <param name="statusCode">
	/// 	The HTTP status code.
	/// </param>
	/// <param name="requestId">
	/// 	The request identifier from the response headers.
	/// </param>
	/// <param name="details">
	/// 	Optional redacted response details.
	/// 	The text can contain application data.
	/// </param>
	/// <param name="retryAfter">
	/// 	The delay that the API suggests.
	/// </param>
	public ApiException(int attempts, HttpStatusCode statusCode, string? requestId = null, string? details = null, TimeSpan? retryAfter = null)
		: base($"The TypeSafe API returned HTTP {(int)statusCode} after {attempts} attempt(s).", attempts)
	{
		StatusCode = statusCode;
		RequestId = requestId;
		Details = details;
		RetryAfter = retryAfter;
	}

	/// <summary>
	/// 	Gets the HTTP status code.
	/// </summary>
	public HttpStatusCode StatusCode { get; }

	/// <summary>
	/// 	Gets the request identifier from the response headers.
	/// </summary>
	public string? RequestId { get; }

	/// <summary>
	/// 	Gets bounded, redacted response details.
	/// 	Treat them as sensitive application data.
	/// </summary>
	public string? Details { get; }

	/// <summary>
	/// 	Gets the valid Retry-After delay, when present.
	/// </summary>
	public TimeSpan? RetryAfter { get; }
}
