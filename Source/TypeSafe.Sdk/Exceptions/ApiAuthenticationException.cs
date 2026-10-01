using System.Net;

namespace TypeSafe.Sdk;

/// <summary>
/// 	The API rejected the credentials (HTTP 401).
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
public sealed class ApiAuthenticationException(int attempts, string? requestId = null, string? details = null)
	: ApiException(attempts, HttpStatusCode.Unauthorized, requestId, details);
