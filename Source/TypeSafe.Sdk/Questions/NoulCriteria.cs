namespace TypeSafe.Sdk;

/// <summary>
/// 	Optional descriptions of the two outcomes of a noul question.
/// </summary>
/// <param name="True">
/// 	A yes description.
/// 	Null omits it, and <see cref="Entry.Null" /> sends JSON null.
/// </param>
/// <param name="False">
/// 	A no description.
/// 	Null omits it, and <see cref="Entry.Null" /> sends JSON null.
/// </param>
public sealed record NoulCriteria(Entry? True = null, Entry? False = null);
