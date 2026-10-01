using System.Text.Json;

namespace TypeSafe.Sdk;

/// <summary>
/// 	A successful System One response with one answer per question.
/// </summary>
public sealed record SystemOneResponse
{
	/// <summary>
	/// 	Gets the answers, keyed by the question names of the request.
	/// </summary>
	public required IReadOnlyDictionary<string, Answer> Answers { get; init; }

	/// <summary>
	/// 	Gets the model that answered the request.
	/// </summary>
	public required string Model { get; init; }

	/// <summary>
	/// 	Gets the token usage of the request.
	/// </summary>
	public required Usage Usage { get; init; }

	/// <summary>
	/// 	Gets the request identifier from the response headers, when present.
	/// </summary>
	public string? RequestId { get; init; }

	/// <summary>
	/// 	Gets top-level fields that the response type does not model.
	/// </summary>
	public IReadOnlyDictionary<string, JsonElement> AdditionalData { get; init; } = new Dictionary<string, JsonElement>().AsReadOnly();

	/// <summary>
	/// 	Gets the answers to choice questions.
	/// </summary>
	public IReadOnlyDictionary<string, ChoiceAnswer> Choices => OfType<ChoiceAnswer>();

	/// <summary>
	/// 	Gets the answers to score questions.
	/// </summary>
	public IReadOnlyDictionary<string, ScoreAnswer> Scores => OfType<ScoreAnswer>();

	/// <summary>
	/// 	Gets the answers to noul questions.
	/// </summary>
	public IReadOnlyDictionary<string, NoulAnswer> Nouls => OfType<NoulAnswer>();

	/// <summary>
	/// 	Gets an answer by question name and checks its type.
	/// </summary>
	/// <typeparam name="TAnswer">
	/// 	The expected answer type.
	/// </typeparam>
	/// <param name="questionName">
	/// 	The exact question name.
	/// </param>
	/// <returns>
	/// 	The matching answer.
	/// </returns>
	/// <exception cref="ArgumentException">
	/// 	The name is empty.
	/// </exception>
	/// <exception cref="KeyNotFoundException">
	/// 	No answer has the name.
	/// </exception>
	/// <exception cref="InvalidOperationException">
	/// 	The answer has a different type.
	/// </exception>
	public TAnswer GetAnswer<TAnswer>(string questionName) where TAnswer : Answer
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(questionName);
		return Answers[questionName] as TAnswer ?? throw new InvalidOperationException($"The answer is not a {typeof(TAnswer).Name}.");
	}

	private IReadOnlyDictionary<string, TAnswer> OfType<TAnswer>() where TAnswer : Answer
	{
		return Answers
			.Where(pair => pair.Value is TAnswer)
			.ToDictionary(pair => pair.Key, pair => (TAnswer)pair.Value, StringComparer.Ordinal)
			.AsReadOnly();
	}
}
