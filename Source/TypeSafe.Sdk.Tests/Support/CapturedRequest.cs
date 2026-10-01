namespace TypeSafe.Sdk.Tests.Support;

/// <summary>
/// 	An HTTP request that the stub handler received.
/// </summary>
internal sealed record CapturedRequest(Uri Uri, string Method, string Body, IReadOnlyDictionary<string, string> Headers);
