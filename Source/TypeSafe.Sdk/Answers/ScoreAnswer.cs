namespace TypeSafe.Sdk;

/// <summary>
/// 	The expected score of a score question.
/// </summary>
/// <param name="Score">
/// 	The expected score.
/// 	The value can fall between integer levels.
/// </param>
/// <param name="Confidence">
/// 	The reported confidence in the score, from zero to one.
/// </param>
/// <param name="Legend">
/// 	The level descriptions, keyed by level index.
/// </param>
/// <param name="Probabilities">
/// 	The reported probability of each level, keyed by level index.
/// </param>
public sealed record ScoreAnswer(double Score, double Confidence, IReadOnlyDictionary<string, Entry> Legend, IReadOnlyDictionary<string, double> Probabilities) : Answer;
