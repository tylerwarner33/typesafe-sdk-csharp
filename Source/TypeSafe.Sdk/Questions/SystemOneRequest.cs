namespace TypeSafe.Sdk;

/// <summary>
/// 	One shared state and one or more independent questions.
/// 	The client sends them in one HTTP request per attempt.
/// </summary>
/// <param name="State">
/// 	A string, JSON object, or JSON array.
/// </param>
/// <param name="Questions">
/// 	Questions keyed by unique, case-sensitive names.
/// </param>
public sealed record SystemOneRequest(Entry State, IReadOnlyDictionary<string, Question> Questions)
{
	/// <summary>
	/// 	Gets the model for this request.
	/// 	Null selects the client default model.
	/// </summary>
	public string? Model { get; init; }
}
