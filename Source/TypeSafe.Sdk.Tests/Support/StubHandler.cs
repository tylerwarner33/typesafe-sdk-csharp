using System.Collections.Concurrent;
using System.Net;
using System.Text;

namespace TypeSafe.Sdk.Tests.Support;

/// <summary>
/// 	A deterministic HTTP transport that records every request.
/// </summary>
internal sealed class StubHandler : HttpMessageHandler
{
	private readonly Func<int, CancellationToken, Task<HttpResponseMessage>> _respond;
	private int _attempts;

	internal StubHandler(Func<int, CancellationToken, Task<HttpResponseMessage>> respond)
	{
		_respond = respond;
	}

	internal StubHandler(string body, HttpStatusCode status = HttpStatusCode.OK)
		: this((_, _) => Task.FromResult(Json(body, status)))
	{
	}

	internal ConcurrentQueue<CapturedRequest> Requests { get; } = new();

	internal bool Disposed { get; private set; }

	internal static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK)
	{
		HttpResponseMessage response = new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
		response.Headers.Add("x-request-id", "request-42");
		return response;
	}

	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		int number = Interlocked.Increment(ref _attempts);
		string body = await request.Content!.ReadAsStringAsync(cancellationToken);
		Dictionary<string, string> headers = request.Headers.Concat(request.Content.Headers)
			.ToDictionary(header => header.Key, header => string.Join(",", header.Value), StringComparer.OrdinalIgnoreCase);
		Requests.Enqueue(new(request.RequestUri!, request.Method.Method, body, headers));
		return await _respond(number, cancellationToken);
	}

	protected override void Dispose(bool disposing)
	{
		Disposed = true;
		base.Dispose(disposing);
	}
}
