namespace TypeSafe.Sdk;

/// <summary>
/// 	The base exception for SDK configuration, transport, and API failures.
/// 	Messages exclude request and response bodies.
/// </summary>
public class TypeSafeException : Exception
{
	/// <summary>
	/// 	Initializes an SDK failure.
	/// </summary>
	/// <param name="message">
	/// 	A message that excludes request and response bodies.
	/// </param>
	/// <param name="attempts">
	/// 	The number of attempts made.
	/// 	Zero means that no request was sent.
	/// </param>
	/// <param name="innerException">
	/// 	An optional underlying exception.
	/// </param>
	public TypeSafeException(string message, int attempts = 0, Exception? innerException = null) : base(message, innerException)
	{
		Attempts = attempts;
	}

	/// <summary>
	/// 	Gets the number of attempts made.
	/// </summary>
	public int Attempts { get; }
}
