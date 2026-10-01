using System.Text.Json;
using TypeSafe.Sdk.Tests.Support;

namespace TypeSafe.Sdk.Tests;

public sealed class SystemOneRequestTests
{
	[Fact]
	public async Task Sends_structured_state_and_questions_without_double_encoding()
	{
		StubHandler handler = new(Fixtures.Standard);
		using (TypeSafeClient client = Fixtures.Client(handler))
		{
			await client.SystemOneAsync(Fixtures.Mixed());
		}

		CapturedRequest captured = Assert.Single(handler.Requests);
		Assert.Equal("POST", captured.Method);
		Assert.Equal("https://api.typesafe.ai/v1/systemone", captured.Uri.AbsoluteUri);
		Assert.Equal("Bearer " + Fixtures.ApiKey, captured.Headers["Authorization"]);
		Assert.Equal("0", captured.Headers["X-TypeSafe-Retry-Count"]);
		Assert.StartsWith("typesafe-sdk-csharp/", captured.Headers["User-Agent"]);
		using (JsonDocument body = JsonDocument.Parse(captured.Body))
		{
			JsonElement root = body.RootElement;
			Assert.Equal(TypeSafeModels.Latest, root.GetProperty("model").GetString());
			Assert.Equal(JsonValueKind.Object, root.GetProperty("state").ValueKind);
			JsonElement questions = root.GetProperty("questions");
			Assert.Equal("choice", questions.GetProperty("department").GetProperty("type").GetString());
			Assert.Equal(JsonValueKind.Null, questions.GetProperty("department").GetProperty("criteria").GetProperty("technical").ValueKind);
			Assert.Equal("score", questions.GetProperty("urgency").GetProperty("type").GetString());
			Assert.Equal(3, questions.GetProperty("urgency").GetProperty("criteria").GetArrayLength());
			Assert.False(questions.GetProperty("refund_requested").TryGetProperty("criteria", out JsonElement _));
			JsonElement criteria = questions.GetProperty("policy_supports_refund").GetProperty("criteria");
			Assert.Equal(JsonValueKind.Object, criteria.GetProperty("true").ValueKind);
			Assert.Equal(JsonValueKind.Null, criteria.GetProperty("false").ValueKind);
		}
	}

	[Fact]
	public async Task Per_request_model_overrides_the_default()
	{
		StubHandler handler = new(Fixtures.Single);
		using (TypeSafeClient client = Fixtures.Client(handler))
		{
			await client.SystemOneAsync(Fixtures.One() with { Model = "custom-model" });
		}

		using (JsonDocument body = JsonDocument.Parse(Assert.Single(handler.Requests).Body))
		{
			Assert.Equal("custom-model", body.RootElement.GetProperty("model").GetString());
		}
	}

	[Fact]
	public async Task Extension_overload_builds_the_request()
	{
		StubHandler handler = new(Fixtures.Single);
		using (TypeSafeClient client = Fixtures.Client(handler))
		{
			SystemOneResponse response = await client.SystemOneAsync(
				"A refund request",
				new Dictionary<string, Question> { ["yes"] = Question.Noul("Refund?") },
				TypeSafeModels.Preview);
			Assert.Equal(0.9, response.Nouls["yes"].Noul);
		}

		using (JsonDocument body = JsonDocument.Parse(Assert.Single(handler.Requests).Body))
		{
			Assert.Equal(TypeSafeModels.Preview, body.RootElement.GetProperty("model").GetString());
		}
	}

	[Fact]
	public async Task Default_headers_and_base_url_are_applied()
	{
		StubHandler handler = new(Fixtures.Single);
		TypeSafeClientOptions options = Fixtures.Options();
		options.BaseUrl = new("https://example.test/proxy/");
		options.DefaultHeaders["X-Team"] = "platform";
		using (TypeSafeClient client = Fixtures.Client(handler, options))
		{
			await client.SystemOneAsync(Fixtures.One());
		}

		CapturedRequest captured = Assert.Single(handler.Requests);
		Assert.Equal("https://example.test/proxy/v1/systemone", captured.Uri.AbsoluteUri);
		Assert.Equal("platform", captured.Headers["X-Team"]);
	}

	[Fact]
	public async Task Invalid_requests_fail_before_any_network_activity()
	{
		StubHandler handler = new(Fixtures.Single);
		using (TypeSafeClient client = Fixtures.Client(handler))
		{
			await Assert.ThrowsAsync<ArgumentException>(() => client.SystemOneAsync(new("state", new Dictionary<string, Question>())));
			await Assert.ThrowsAsync<ArgumentException>(() => client.SystemOneAsync(new("state", new Dictionary<string, Question>
			{
				["score"] = Question.Score("Rate", ["only one level"])
			})));
			await Assert.ThrowsAsync<ArgumentException>(() => client.SystemOneAsync(new("state", new Dictionary<string, Question>
			{
				["choice"] = Question.Choice("Pick", new Dictionary<string, Entry>())
			})));
			await Assert.ThrowsAsync<ArgumentException>(() => client.SystemOneAsync(new("state", new Dictionary<string, Question>
			{
				[" "] = Question.Noul("Blank name")
			})));
			await Assert.ThrowsAsync<ArgumentException>(() => client.SystemOneAsync(Fixtures.One() with { Model = " " }));
			await Assert.ThrowsAsync<ArgumentException>(() => client.SystemOneAsync(new(Entry.FromJson("42"), new Dictionary<string, Question>
			{
				["yes"] = Question.Noul("Number state")
			})));
		}

		Assert.Empty(handler.Requests);
	}
}
