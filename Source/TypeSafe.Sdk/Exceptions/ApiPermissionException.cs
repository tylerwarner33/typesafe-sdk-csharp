using System.Net;

namespace TypeSafe.Sdk;

/// <summary>
/// 	The credentials lack permission for the request (HTTP 403).
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
public sealed class ApiPermissionException(int attempts, string? requestId = null, string? details = null)
	: ApiException(attempts, HttpStatusCode.Forbidden, requestId, details);
