namespace TypeSafe.Sdk;

/// <summary>
/// 	A question that rates the state against ordered levels indexed from zero.
/// </summary>
/// <param name="Instructions">
/// 	The judgment to make.
/// </param>
/// <param name="Criteria">
/// 	Ordered level descriptions.
/// 	A question holds between 2 and 10 levels.
/// </param>
public sealed record ScoreQuestion(Entry Instructions, IReadOnlyList<Entry> Criteria) : Question(Instructions);
