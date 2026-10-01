namespace TypeSafe.Sdk;

/// <summary>
/// 	Sends System One requests to the TypeSafe API.
/// </summary>
public interface ITypeSafeClient : IDisposable
{
	/// <summary>
	/// 	Answers every question about the same state in one request per attempt.
	/// </summary>
	/// <param name="request">
	/// 	The state, the questions, and an optional model.
	/// </param>
	/// <param name="cancellationToken">
	/// 	Cancels HTTP operations and retry waits.
	/// </param>
	/// <returns>
	/// 	The answers, keyed by question name.
	/// </returns>
	/// <exception cref="ArgumentException">
	/// 	The request is invalid.
	/// </exception>
	/// <exception cref="ApiException">
	/// 	The API rejects the request.
	/// </exception>
	/// <exception cref="ApiConnectionException">
	/// 	A connection failure uses all attempts.
	/// </exception>
	/// <exception cref="ApiTimeoutException">
	/// 	An attempt times out and no attempts remain.
	/// </exception>
	/// <exception cref="InvalidResponseException">
	/// 	The response does not match the request.
	/// </exception>
	/// <exception cref="OperationCanceledException">
	/// 	The caller cancels the request.
	/// </exception>
	/// <exception cref="ObjectDisposedException">
	/// 	The client is disposed.
	/// </exception>
	Task<SystemOneResponse> SystemOneAsync(SystemOneRequest request, CancellationToken cancellationToken = default);
}
