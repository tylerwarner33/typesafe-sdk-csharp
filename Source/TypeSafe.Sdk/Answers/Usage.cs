using System.Text.Json;

namespace TypeSafe.Sdk;

/// <summary>
/// 	Token usage that the API reports for a request.
/// </summary>
/// <param name="InputTokens">
/// 	The number of input tokens.
/// </param>
/// <param name="OutputTokens">
/// 	The number of output tokens.
/// </param>
public sealed record Usage(long InputTokens, long OutputTokens)
{
	/// <summary>
	/// 	Gets additional usage fields that the API reports.
	/// </summary>
	public IReadOnlyDictionary<string, JsonElement> AdditionalData { get; init; } = new Dictionary<string, JsonElement>().AsReadOnly();
}
