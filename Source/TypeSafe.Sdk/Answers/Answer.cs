using System.Text.Json;

namespace TypeSafe.Sdk;

/// <summary>
/// 	The common base of typed answers.
/// </summary>
public abstract record Answer
{
	/// <summary>
	/// 	Gets API fields that the typed answer does not model.
	/// </summary>
	public IReadOnlyDictionary<string, JsonElement> AdditionalData { get; init; } = new Dictionary<string, JsonElement>().AsReadOnly();
}
