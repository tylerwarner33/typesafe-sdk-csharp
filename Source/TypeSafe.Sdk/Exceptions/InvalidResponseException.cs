namespace TypeSafe.Sdk;

/// <summary>
/// 	A successful response violated the System One contract.
/// </summary>
/// <param name="attempts">
/// 	The number of attempts made.
/// </param>
/// <param name="requestId">
/// 	The request identifier from the response headers.
/// </param>
public sealed class InvalidResponseException(int attempts, string? requestId = null)
	: TypeSafeException("The TypeSafe API returned an invalid System One response.", attempts)
{
	/// <summary>
	/// 	Gets the request identifier from the response headers.
	/// </summary>
	public string? RequestId { get; } = requestId;
}
