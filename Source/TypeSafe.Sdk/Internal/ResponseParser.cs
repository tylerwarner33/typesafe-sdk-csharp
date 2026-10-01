using System.Globalization;
using System.Text.Json;

namespace TypeSafe.Sdk.Internal;

/// <summary>
/// 	Parses and validates a System One response against the serialized request.
/// </summary>
internal static class ResponseParser
{
	private static readonly string[] _answerFields = ["type", "choice", "score", "noul", "confidence", "probabilities", "legend"];
	private static readonly string[] _usageFields = ["input_tokens", "output_tokens"];
	private static readonly string[] _responseFields = ["answers", "model", "usage"];

	/// <summary>
	/// 	Parses a response body.
	/// 	Any contract violation becomes an <see cref="InvalidResponseException" />.
	/// </summary>
	internal static SystemOneResponse Parse(byte[] body, PreparedRequest request, int attempts, string? requestId)
	{
		try
		{
			using (JsonDocument document = JsonDocument.Parse(body))
			{
				JsonElement root = document.RootElement;
				RejectDuplicates(root);
				Dictionary<string, Answer> answers = new(StringComparer.Ordinal);
				foreach (JsonProperty property in root.GetProperty("answers").EnumerateObject())
				{
					if (request.Questions.TryGetProperty(property.Name, out JsonElement question) is false)
					{
						throw new JsonException();
					}

					answers.Add(property.Name, ParseAnswer(property.Value, question));
				}

				if (answers.Count != request.Questions.EnumerateObject().Count())
				{
					throw new JsonException();
				}

				return new()
				{
					Answers = answers.AsReadOnly(),
					Model = root.GetProperty("model").GetString() ?? throw new JsonException(),
					Usage = ParseUsage(root.GetProperty("usage")),
					RequestId = requestId,
					AdditionalData = Extra(root, _responseFields)
				};
			}
		}
		catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException or ArgumentException)
		{
			throw new InvalidResponseException(attempts, requestId);
		}
	}

	private static Answer ParseAnswer(JsonElement answer, JsonElement question)
	{
		string? type = answer.GetProperty("type").GetString();
		if (type != question.GetProperty("type").GetString())
		{
			throw new JsonException();
		}

		double confidence;
		Answer parsed;
		switch (type)
		{
			case "choice":
				string selected = answer.GetProperty("choice").GetString() ?? throw new JsonException();
				HashSet<string> choices = question.GetProperty("criteria").EnumerateObject().Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
				if (choices.Contains(selected) is false)
				{
					throw new JsonException();
				}

				confidence = Number(answer.GetProperty("confidence"), 0, 1);
				parsed = new ChoiceAnswer(selected, confidence, Distribution(answer, choices));
				break;
			case "score":
				int levelCount = question.GetProperty("criteria").GetArrayLength();
				HashSet<string> levels = Enumerable.Range(0, levelCount).Select(number => number.ToString(CultureInfo.InvariantCulture)).ToHashSet(StringComparer.Ordinal);
				double score = Number(answer.GetProperty("score"), 0, levelCount - 1);
				confidence = Number(answer.GetProperty("confidence"), 0, 1);
				Dictionary<string, Entry> legend = answer.GetProperty("legend").EnumerateObject()
					.ToDictionary(property => property.Name, property => Entry.FromJson(property.Value.GetRawText()), StringComparer.Ordinal);
				if (levels.SetEquals(legend.Keys) is false)
				{
					throw new JsonException();
				}

				parsed = new ScoreAnswer(score, confidence, legend.AsReadOnly(), Distribution(answer, levels));
				break;
			case "noul":
				parsed = new NoulAnswer(Number(answer.GetProperty("noul"), 0, 1));
				break;
			default:
				throw new JsonException();
		}

		return parsed with { AdditionalData = Extra(answer, _answerFields) };
	}

	private static Usage ParseUsage(JsonElement usage)
	{
		return new(Count(usage.GetProperty("input_tokens")), Count(usage.GetProperty("output_tokens")))
		{
			AdditionalData = Extra(usage, _usageFields)
		};
	}

	/// <summary>
	/// 	Rejects ambiguous duplicate JSON properties, including nested answers.
	/// </summary>
	private static void RejectDuplicates(JsonElement value)
	{
		if (value.ValueKind == JsonValueKind.Object)
		{
			HashSet<string> names = new(StringComparer.Ordinal);
			foreach (JsonProperty property in value.EnumerateObject())
			{
				if (names.Add(property.Name) is false)
				{
					throw new JsonException();
				}

				RejectDuplicates(property.Value);
			}
		}
		else if (value.ValueKind == JsonValueKind.Array)
		{
			foreach (JsonElement child in value.EnumerateArray())
			{
				RejectDuplicates(child);
			}
		}
	}

	private static IReadOnlyDictionary<string, double> Distribution(JsonElement answer, HashSet<string> allowed)
	{
		Dictionary<string, double> values = answer.GetProperty("probabilities").EnumerateObject()
			.ToDictionary(property => property.Name, property => Number(property.Value, 0, 1), StringComparer.Ordinal);
		if (allowed.SetEquals(values.Keys) is false)
		{
			throw new JsonException();
		}

		return values.AsReadOnly();
	}

	private static double Number(JsonElement value, double minimum, double maximum)
	{
		double number = value.GetDouble();
		return double.IsFinite(number) && number >= minimum && number <= maximum ? number : throw new JsonException();
	}

	private static long Count(JsonElement value)
	{
		long number = value.GetInt64();
		return number >= 0 ? number : throw new JsonException();
	}

	/// <summary>
	/// 	Keeps unknown JSON properties independent of the source document.
	/// </summary>
	private static IReadOnlyDictionary<string, JsonElement> Extra(JsonElement value, string[] known)
	{
		return value.EnumerateObject()
			.Where(property => known.Contains(property.Name, StringComparer.Ordinal) is false)
			.ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.Ordinal)
			.AsReadOnly();
	}
}
