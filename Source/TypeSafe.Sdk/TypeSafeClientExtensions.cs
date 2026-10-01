namespace TypeSafe.Sdk;

/// <summary>
/// 	Convenience overloads for <see cref="ITypeSafeClient" />.
/// </summary>
public static class TypeSafeClientExtensions
{
	/// <summary>
	/// 	Answers questions about a state without building a request object.
	/// </summary>
	/// <param name="client">
	/// 	The client that sends the request.
	/// </param>
	/// <param name="state">
	/// 	A string, JSON object, or JSON array.
	/// </param>
	/// <param name="questions">
	/// 	Questions keyed by unique, case-sensitive names.
	/// </param>
	/// <param name="model">
	/// 	An optional model.
	/// 	Null selects the client default model.
	/// </param>
	/// <param name="cancellationToken">
	/// 	Cancels HTTP operations and retry waits.
	/// </param>
	/// <returns>
	/// 	The answers, keyed by question name.
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// 	The client is null.
	/// </exception>
	public static Task<SystemOneResponse> SystemOneAsync(
		this ITypeSafeClient client,
		Entry state,
		IReadOnlyDictionary<string, Question> questions,
		string? model = null,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(client);
		return client.SystemOneAsync(new SystemOneRequest(state, questions) { Model = model }, cancellationToken);
	}
}
