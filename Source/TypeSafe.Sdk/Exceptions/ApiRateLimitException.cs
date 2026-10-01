using System.Net;

namespace TypeSafe.Sdk;

/// <summary>
/// 	The API rate limited the request (HTTP 429).
/// </summary>
/// <param name="attempts">
/// 	The number of attempts made.
/// </param>
/// <param name="requestId">
/// 	The request identifier from the response headers.
/// </param>
/// <param name="details">
/// 	Optional redacted response details.
/// </param>
/// <param name="retryAfter">
/// 	The delay that the API suggests.
/// </param>
public sealed class ApiRateLimitException(int attempts, string? requestId = null, string? details = null, TimeSpan? retryAfter = null)
	: ApiException(attempts, HttpStatusCode.TooManyRequests, requestId, details, retryAfter);
