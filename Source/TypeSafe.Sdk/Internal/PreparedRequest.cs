using System.Text.Json;

namespace TypeSafe.Sdk.Internal;

/// <summary>
/// 	A stable body and question snapshot that retries and response validation share.
/// </summary>
/// <param name="Model">
/// 	The model sent in the request.
/// </param>
/// <param name="Body">
/// 	The serialized JSON body.
/// </param>
/// <param name="Questions">
/// 	The serialized question map.
/// </param>
internal sealed record PreparedRequest(string Model, byte[] Body, JsonElement Questions);
