using System.Net;

namespace TypeSafe.Sdk.Internal;

/// <summary>
/// 	Decides which failures deserve another attempt.
/// </summary>
internal static class HttpFailureClassifier
{
	/// <summary>
	/// 	Identifies HTTP statuses that are eligible for another attempt.
	/// </summary>
	internal static bool IsTransient(HttpStatusCode status)
	{
		return (int)status is 408 or 429 or (>= 500 and < 600);
	}

	/// <summary>
	/// 	Identifies recoverable failures while the client sends or reads HTTP data.
	/// </summary>
	internal static bool IsTransient(Exception? exception)
	{
		return exception is IOException
			or HttpRequestException { HttpRequestError: HttpRequestError.ConnectionError or HttpRequestError.NameResolutionError or HttpRequestError.ResponseEnded };
	}
}
