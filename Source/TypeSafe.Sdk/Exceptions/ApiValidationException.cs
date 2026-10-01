using System.Net;

namespace TypeSafe.Sdk;

/// <summary>
/// 	The API rejected the request as invalid (HTTP 400 or 422).
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
/// </param>
public sealed class ApiValidationException(int attempts, HttpStatusCode statusCode, string? requestId = null, string? details = null)
	: ApiException(attempts, statusCode, requestId, details);
