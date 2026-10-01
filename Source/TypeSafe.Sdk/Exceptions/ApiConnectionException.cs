namespace TypeSafe.Sdk;

/// <summary>
/// 	The client could not reach the API after the configured attempts.
/// </summary>
/// <param name="attempts">
/// 	The number of attempts made.
/// </param>
/// <param name="isTransient">
/// 	Whether the failure is transient and a retry is useful.
/// </param>
public sealed class ApiConnectionException(int attempts, bool isTransient = false)
	: TypeSafeException($"Could not reach the TypeSafe API after {attempts} attempt(s).", attempts)
{
	/// <summary>
	/// 	Gets whether the client classified the failure as transient.
	/// </summary>
	public bool IsTransient { get; } = isTransient;
}
