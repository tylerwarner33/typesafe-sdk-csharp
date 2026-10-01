namespace TypeSafe.Sdk;

/// <summary>
/// 	The selected option of a choice question.
/// </summary>
/// <param name="Choice">
/// 	The selected option name.
/// </param>
/// <param name="Confidence">
/// 	The reported confidence in the selected option, from zero to one.
/// </param>
/// <param name="Probabilities">
/// 	The reported probability of each option.
/// </param>
public sealed record ChoiceAnswer(string Choice, double Confidence, IReadOnlyDictionary<string, double> Probabilities) : Answer;
