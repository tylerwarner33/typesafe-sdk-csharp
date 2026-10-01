namespace TypeSafe.Sdk;

/// <summary>
/// 	A yes/no question that returns the probability of yes.
/// </summary>
/// <param name="Instructions">
/// 	The proposition to evaluate.
/// </param>
/// <param name="Criteria">
/// 	Optional descriptions of yes and no.
/// </param>
public sealed record NoulQuestion(Entry Instructions, NoulCriteria? Criteria = null) : Question(Instructions);
