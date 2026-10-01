using System.Text.Json;

namespace TypeSafe.Sdk.Internal;

/// <summary>
/// 	Validates a request and serializes it to the System One wire format.
/// </summary>
internal static class RequestSerializer
{
	/// <summary>
	/// 	Validates and serializes a request once for all attempts.
	/// </summary>
	internal static PreparedRequest Prepare(SystemOneRequest request, string defaultModel)
	{
		ArgumentNullException.ThrowIfNull(request);
		string model = request.Model ?? defaultModel;
		ValidateModel(model);
		ValidateEntry(request.State, false);
		if (request.Questions is null || request.Questions.Count == 0)
		{
			throw new ArgumentException("At least one question is required.", nameof(request));
		}

		using (MemoryStream stream = new())
		{
			using (Utf8JsonWriter writer = new(stream))
			{
				writer.WriteStartObject();
				writer.WriteString("model", model);
				WriteEntry(writer, "state", request.State);
				writer.WriteStartObject("questions");
				HashSet<string> names = new(StringComparer.Ordinal);
				foreach (KeyValuePair<string, Question> pair in request.Questions)
				{
					if (string.IsNullOrWhiteSpace(pair.Key) || names.Add(pair.Key) is false || pair.Value is null)
					{
						throw new ArgumentException("Question names must be unique and nonempty, and questions cannot be null.", nameof(request));
					}

					WriteQuestion(writer, pair.Key, pair.Value);
				}

				writer.WriteEndObject();
				writer.WriteEndObject();
			}

			byte[] body = stream.ToArray();
			using (JsonDocument snapshot = JsonDocument.Parse(body))
			{
				return new(model, body, snapshot.RootElement.GetProperty("questions").Clone());
			}
		}
	}

	/// <summary>
	/// 	Rejects empty model names and names with characters that are unsafe in logs.
	/// </summary>
	internal static void ValidateModel(string model)
	{
		if (string.IsNullOrWhiteSpace(model) || model.Any(char.IsControl))
		{
			throw new ArgumentException("A model name must be nonempty and contain no control characters.", nameof(model));
		}
	}

	private static void WriteQuestion(Utf8JsonWriter writer, string name, Question question)
	{
		ValidateEntry(question.Instructions, true);
		writer.WriteStartObject(name);
		WriteEntry(writer, "instructions", question.Instructions);
		switch (question)
		{
			case ChoiceQuestion choice:
				WriteChoice(writer, choice);
				break;
			case ScoreQuestion score:
				WriteScore(writer, score);
				break;
			case NoulQuestion noul:
				WriteNoul(writer, noul);
				break;
			default:
				throw new ArgumentException("Unsupported question type.", nameof(question));
		}

		writer.WriteEndObject();
	}

	private static void WriteChoice(Utf8JsonWriter writer, ChoiceQuestion choice)
	{
		if (choice.Criteria is null || choice.Criteria.Count is < 1 or > 255)
		{
			throw new ArgumentException("A choice question requires between 1 and 255 options.", nameof(choice));
		}

		writer.WriteString("type", "choice");
		writer.WriteStartObject("criteria");
		HashSet<string> options = new(StringComparer.Ordinal);
		foreach (KeyValuePair<string, Entry> pair in choice.Criteria)
		{
			if (string.IsNullOrWhiteSpace(pair.Key) || options.Add(pair.Key) is false)
			{
				throw new ArgumentException("Choice option names must be unique and nonempty.", nameof(choice));
			}

			ValidateEntry(pair.Value, true);
			WriteEntry(writer, pair.Key, pair.Value);
		}

		writer.WriteEndObject();
	}

	private static void WriteScore(Utf8JsonWriter writer, ScoreQuestion score)
	{
		if (score.Criteria is null || score.Criteria.Count is < 2 or > 10)
		{
			throw new ArgumentException("A score question requires between 2 and 10 ordered levels.", nameof(score));
		}

		writer.WriteString("type", "score");
		writer.WriteStartArray("criteria");
		foreach (Entry level in score.Criteria)
		{
			ValidateEntry(level, true);
			level.Json.WriteTo(writer);
		}

		writer.WriteEndArray();
	}

	private static void WriteNoul(Utf8JsonWriter writer, NoulQuestion noul)
	{
		writer.WriteString("type", "noul");
		if (noul.Criteria is null)
		{
			return;
		}

		writer.WriteStartObject("criteria");
		if (noul.Criteria.True is not null)
		{
			ValidateEntry(noul.Criteria.True, true);
			WriteEntry(writer, "true", noul.Criteria.True);
		}

		if (noul.Criteria.False is not null)
		{
			ValidateEntry(noul.Criteria.False, true);
			WriteEntry(writer, "false", noul.Criteria.False);
		}

		writer.WriteEndObject();
	}

	/// <summary>
	/// 	Checks an entry without restricting the types of nested JSON fields.
	/// </summary>
	private static void ValidateEntry(Entry? entry, bool nullable)
	{
		bool valid = entry is not null
			&& (entry.Json.ValueKind is JsonValueKind.String or JsonValueKind.Object or JsonValueKind.Array
				|| (nullable && entry.Json.ValueKind == JsonValueKind.Null));
		if (valid is false)
		{
			throw new ArgumentException("An entry must be a string, object, or array. Use Entry.Null where null is supported.");
		}
	}

	private static void WriteEntry(Utf8JsonWriter writer, string name, Entry entry)
	{
		writer.WritePropertyName(name);
		entry.Json.WriteTo(writer);
	}
}
