namespace TypeSafe.Sdk;

/// <summary>
/// 	A question that selects one named option.
/// </summary>
/// <param name="Instructions">
/// 	The judgment to make.
/// </param>
/// <param name="Criteria">
/// 	Option names and their descriptions, including explicit JSON null.
/// 	A question holds between 1 and 255 options.
/// </param>
public sealed record ChoiceQuestion(Entry Instructions, IReadOnlyDictionary<string, Entry> Criteria) : Question(Instructions);
