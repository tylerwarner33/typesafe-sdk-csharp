namespace TypeSafe.Sdk.Samples.Console;

/// <summary>
/// 	Sends one mixed-question System One request from the command line.
/// </summary>
internal static class Program
{
	private const string State = "I was charged twice. Please fix this ASAP.";

	/// <summary>
	/// 	Reads the API key from <c>TYPESAFE_API_KEY</c>, asks three questions about a support ticket, and writes the answers.
	/// </summary>
	/// <param name="args">
	/// 	Unused command-line arguments.
	/// </param>
	/// <returns>
	/// 	Zero on success, otherwise one.
	/// </returns>
	private static async Task<int> Main(string[] args)
	{
		using (TypeSafeClient client = new())
		{
			using (CancellationTokenSource cancellation = new(TimeSpan.FromMinutes(2)))
			{
				Dictionary<string, Question> questions = new()
				{
					["category"] = Question.Choice("What is this ticket about?", new Dictionary<string, Entry>
					{
						["billing"] = "Charges, payments, and refunds",
						["technical"] = "Software defects and outages",
						["other"] = Entry.Null
					}),
					["urgency"] = Question.Score("How urgently should support respond?", ["Routine", "Soon", "Immediately"]),
					["refund_requested"] = Question.Noul("Does the customer ask for a refund?")
				};
				try
				{
					SystemOneResponse response = await client.SystemOneAsync(State, questions, cancellationToken: cancellation.Token);
					System.Console.WriteLine($"Category: {response.Choices["category"].Choice}");
					System.Console.WriteLine($"Urgency: {response.Scores["urgency"].Score:F2}");
					System.Console.WriteLine($"Refund requested: {response.Nouls["refund_requested"].Noul:P0}");
					System.Console.WriteLine($"Model: {response.Model}; tokens in/out: {response.Usage.InputTokens}/{response.Usage.OutputTokens}");
					return 0;
				}
				catch (ApiAuthenticationException)
				{
					System.Console.Error.WriteLine("Authentication failed. Check the API key.");
				}
				catch (ApiRateLimitException exception)
				{
					System.Console.Error.WriteLine($"Rate limited after {exception.Attempts} attempt(s). Try again later.");
				}
				catch (TypeSafeException exception)
				{
					System.Console.Error.WriteLine(exception.Message);
				}
				catch (OperationCanceledException)
				{
					System.Console.Error.WriteLine("The request was canceled.");
				}

				return 1;
			}
		}
	}
}
