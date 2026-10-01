using TypeSafe.Sdk.Tests.Support;

namespace TypeSafe.Sdk.Tests;

public sealed class SystemOneResponseTests
{
	[Fact]
	public async Task Parses_all_three_answer_types_and_metadata()
	{
		StubHandler handler = new(Fixtures.Standard);
		SystemOneResponse response;
		using (TypeSafeClient client = Fixtures.Client(handler))
		{
			response = await client.SystemOneAsync(Fixtures.Mixed());
		}

		Assert.Equal("jev-1.13.0", response.Model);
		Assert.Equal("request-42", response.RequestId);
		Assert.Equal(120, response.Usage.InputTokens);
		Assert.Equal(20, response.Usage.OutputTokens);
		Assert.Equal(3, response.Usage.AdditionalData["cache_read_tokens"].GetInt32());
		Assert.True(response.AdditionalData["future"].GetProperty("enabled").GetBoolean());
		Assert.Equal(4, response.Answers.Count);

		ChoiceAnswer department = response.Choices["department"];
		Assert.Equal("billing", department.Choice);
		Assert.Equal(0.7, department.Confidence);
		Assert.Equal(0.8, department.Probabilities["billing"]);
		Assert.Equal(17, department.AdditionalData["futureAnswer"].GetInt32());

		ScoreAnswer urgency = response.Scores["urgency"];
		Assert.Equal(1.6, urgency.Score);
		Assert.Equal("Soon", urgency.Legend["1"].Json.GetString());
		Assert.Equal(0.7, urgency.Probabilities["2"]);

		Assert.Equal(2, response.Nouls.Count);
		Assert.Equal(0.95, response.Nouls["refund_requested"].Noul);
		Assert.Equal(0.8, response.GetAnswer<NoulAnswer>("policy_supports_refund").Noul);
	}

	[Fact]
	public async Task GetAnswer_checks_the_answer_type()
	{
		StubHandler handler = new(Fixtures.Single);
		using (TypeSafeClient client = Fixtures.Client(handler))
		{
			SystemOneResponse response = await client.SystemOneAsync(Fixtures.One());
			Assert.Throws<InvalidOperationException>(() => response.GetAnswer<ChoiceAnswer>("yes"));
			Assert.Throws<KeyNotFoundException>(() => response.GetAnswer<NoulAnswer>("missing"));
		}
	}

	[Theory]
	[InlineData("""{"model":"m","usage":{"input_tokens":1,"output_tokens":1},"answers":{}}""")]
	[InlineData("""{"model":"m","usage":{"input_tokens":1,"output_tokens":1},"answers":{"yes":{"type":"noul","noul":1.5}}}""")]
	[InlineData("""{"model":"m","usage":{"input_tokens":1,"output_tokens":1},"answers":{"yes":{"type":"score","score":0.5}}}""")]
	[InlineData("""{"model":"m","usage":{"input_tokens":1,"output_tokens":1},"answers":{"other":{"type":"noul","noul":0.5}}}""")]
	[InlineData("""{"model":"m","usage":{"input_tokens":1,"output_tokens":1},"answers":{"yes":{"type":"noul","noul":0.5,"noul":0.6}}}""")]
	[InlineData("""{"usage":{"input_tokens":1,"output_tokens":1},"answers":{"yes":{"type":"noul","noul":0.5}}}""")]
	[InlineData("""{"model":"m","answers":{"yes":{"type":"noul","noul":0.5}}}""")]
	[InlineData("not json")]
	public async Task Rejects_invalid_successful_responses_without_retrying(string body)
	{
		StubHandler handler = new(body);
		using (TypeSafeClient client = Fixtures.Client(handler))
		{
			InvalidResponseException exception = await Assert.ThrowsAsync<InvalidResponseException>(() => client.SystemOneAsync(Fixtures.One()));
			Assert.Equal(1, exception.Attempts);
			Assert.Equal("request-42", exception.RequestId);
		}

		Assert.Single(handler.Requests);
	}

	[Fact]
	public async Task Rejects_oversized_responses()
	{
		StubHandler handler = new(Fixtures.Single);
		TypeSafeClientOptions options = Fixtures.Options();
		options.MaxResponseBytes = 16;
		using (TypeSafeClient client = Fixtures.Client(handler, options))
		{
			await Assert.ThrowsAsync<InvalidResponseException>(() => client.SystemOneAsync(Fixtures.One()));
		}
	}
}
