namespace TypeSafe.Sdk;

/// <summary>
/// 	A judgment about the shared state of a request.
/// </summary>
/// <param name="Instructions">
/// 	Text, structured JSON, or an explicit null.
/// </param>
public abstract record Question(Entry Instructions)
{
	/// <summary>
	/// 	Creates a question that selects one named option.
	/// </summary>
	/// <param name="instructions">
	/// 	The judgment to make.
	/// </param>
	/// <param name="criteria">
	/// 	Option names and their descriptions.
	/// </param>
	/// <returns>
	/// 	The choice question.
	/// </returns>
	public static ChoiceQuestion Choice(Entry instructions, IReadOnlyDictionary<string, Entry> criteria)
	{
		return new(instructions, criteria);
	}

	/// <summary>
	/// 	Creates a question that rates the state against ordered levels.
	/// </summary>
	/// <param name="instructions">
	/// 	The judgment to make.
	/// </param>
	/// <param name="criteria">
	/// 	Ordered level descriptions, indexed from zero.
	/// </param>
	/// <returns>
	/// 	The score question.
	/// </returns>
	public static ScoreQuestion Score(Entry instructions, IReadOnlyList<Entry> criteria)
	{
		return new(instructions, criteria);
	}

	/// <summary>
	/// 	Creates a yes/no question.
	/// </summary>
	/// <param name="instructions">
	/// 	The proposition to evaluate.
	/// </param>
	/// <param name="criteria">
	/// 	Optional descriptions of yes and no.
	/// </param>
	/// <returns>
	/// 	The noul question.
	/// </returns>
	public static NoulQuestion Noul(Entry instructions, NoulCriteria? criteria = null)
	{
		return new(instructions, criteria);
	}
}
