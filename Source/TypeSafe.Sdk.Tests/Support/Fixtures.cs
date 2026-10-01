namespace TypeSafe.Sdk.Tests.Support;

/// <summary>
/// 	Shared requests, responses, and client factories.
/// </summary>
internal static class Fixtures
{
	internal const string ApiKey = "test-secret";

	internal const string Standard = """
		{
		  "model":"jev-1.13.0",
		  "usage":{"input_tokens":120,"output_tokens":20,"cache_read_tokens":3},
		  "future":{"enabled":true},
		  "answers":{
		    "policy_supports_refund":{"type":"noul","noul":0.8},
		    "refund_requested":{"type":"noul","noul":0.95},
		    "urgency":{"type":"score","score":1.6,"probabilities":{"0":0.1,"1":0.2,"2":0.7},"legend":{"0":"Routine","1":"Soon","2":"Immediate"},"confidence":0.6},
		    "department":{"type":"choice","choice":"billing","probabilities":{"billing":0.8,"technical":0.2},"confidence":0.7,"futureAnswer":17}
		  }
		}
		""";

	internal const string Single = """{"model":"jev-1.13.0","usage":{"input_tokens":1,"output_tokens":1},"answers":{"yes":{"type":"noul","noul":0.9}}}""";

	internal static SystemOneRequest Mixed()
	{
		return new(
			Entry.FromObject(new { ticket = "Charged twice", amounts = new[] { 49, 49 }, eligible = true }),
			new Dictionary<string, Question>
			{
				["department"] = Question.Choice("Select a team.", new Dictionary<string, Entry>
				{
					["billing"] = Entry.FromObject(new { handles = new[] { "refunds", "charges" } }),
					["technical"] = Entry.Null
				}),
				["urgency"] = Question.Score(
					Entry.FromObject(new[] { "Rate urgency", "Use the rubric" }),
					["Routine", Entry.FromObject(new { label = "Soon" }), "Immediate"]),
				["refund_requested"] = Question.Noul("Was a refund requested?"),
				["policy_supports_refund"] = Question.Noul(
					"Does the policy support a refund?",
					new NoulCriteria(Entry.FromObject(new { explanation = "Duplicate charges" }), Entry.Null))
			});
	}

	internal static SystemOneRequest One()
	{
		return new("A refund request", new Dictionary<string, Question>
		{
			["yes"] = Question.Noul("Does the customer want a refund?")
		});
	}

	/// <summary>
	/// 	Creates options with no retry waits.
	/// 	Explicit values keep TYPESAFE_* environment variables out of the tests.
	/// </summary>
	internal static TypeSafeClientOptions Options(int maxRetries = 2)
	{
		TypeSafeClientOptions options = new()
		{
			ApiKey = ApiKey,
			BaseUrl = new(TypeSafeClientOptions.DefaultBaseUrl),
			DefaultModel = TypeSafeModels.Latest
		};
		options.Retry.MaxRetries = maxRetries;
		options.Retry.InitialBackoff = TimeSpan.Zero;
		options.Retry.MaxBackoff = TimeSpan.Zero;
		options.Retry.Jitter = 0;
		return options;
	}

	internal static TypeSafeClient Client(StubHandler handler, TypeSafeClientOptions? options = null)
	{
		return new(options ?? Options(), new HttpClient(handler, false));
	}

	/// <summary>
	/// 	Waits for an asynchronous checkpoint, with a bounded real-time deadline.
	/// </summary>
	internal static async Task EventuallyAsync(Func<bool> condition)
	{
		for (int index = 0; index < 1000 && condition() is false; index++)
		{
			await Task.Delay(1);
		}

		Assert.True(condition(), "The asynchronous operation did not reach the expected checkpoint.");
	}

	/// <summary>
	/// 	Gives pending continuations a chance to run, so that a test can assert that nothing happened.
	/// </summary>
	internal static async Task SettleAsync()
	{
		await Task.Delay(25);
	}
}
