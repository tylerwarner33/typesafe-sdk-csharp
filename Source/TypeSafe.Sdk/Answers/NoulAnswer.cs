namespace TypeSafe.Sdk;

/// <summary>
/// 	The answer to a yes/no question.
/// </summary>
/// <param name="Noul">
/// 	The probability of yes, from zero to one.
/// 	The SDK applies no threshold.
/// </param>
public sealed record NoulAnswer(double Noul) : Answer;
