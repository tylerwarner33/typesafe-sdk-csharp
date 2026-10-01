using System.Text.Json;

namespace TypeSafe.Sdk;

/// <summary>
/// 	An immutable JSON value used for state, instructions, and criteria.
/// </summary>
public sealed class Entry
{
	private readonly JsonElement _value;

	private Entry(JsonElement value)
	{
		_value = value.Clone();
	}

	/// <summary>
	/// 	Gets an explicit JSON null value.
	/// </summary>
	public static Entry Null { get; } = FromObject<object?>(null);

	/// <summary>
	/// 	Gets the JSON representation.
	/// </summary>
	public JsonElement Json => _value;

	/// <summary>
	/// 	Creates an entry from a CLR object and keeps its JSON structure.
	/// </summary>
	/// <typeparam name="T">
	/// 	The CLR type of the value.
	/// </typeparam>
	/// <param name="value">
	/// 	The value to serialize.
	/// </param>
	/// <param name="options">
	/// 	Optional serialization settings.
	/// </param>
	/// <returns>
	/// 	An immutable JSON entry.
	/// </returns>
	/// <exception cref="JsonException">
	/// 	The object cannot be serialized.
	/// </exception>
	public static Entry FromObject<T>(T value, JsonSerializerOptions? options = null)
	{
		return new(JsonSerializer.SerializeToElement(value, options));
	}

	/// <summary>
	/// 	Parses JSON without encoding it as a JSON string.
	/// </summary>
	/// <param name="json">
	/// 	A complete JSON value.
	/// </param>
	/// <returns>
	/// 	The parsed entry.
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// 	The input is null.
	/// </exception>
	/// <exception cref="JsonException">
	/// 	The input is not valid JSON.
	/// </exception>
	public static Entry FromJson(string json)
	{
		ArgumentNullException.ThrowIfNull(json);
		using (JsonDocument document = JsonDocument.Parse(json))
		{
			return new(document.RootElement);
		}
	}

	/// <summary>
	/// 	Converts text to a JSON string, or null to JSON null.
	/// </summary>
	/// <param name="text">
	/// 	The text to wrap.
	/// </param>
	public static implicit operator Entry(string? text)
	{
		return FromObject(text);
	}

	/// <summary>
	/// 	Returns the JSON representation.
	/// 	The text can contain sensitive application data.
	/// </summary>
	/// <returns>
	/// 	The serialized JSON.
	/// </returns>
	public override string ToString()
	{
		return _value.GetRawText();
	}
}
