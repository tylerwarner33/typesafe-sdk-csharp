namespace TypeSafe.Sdk;

/// <summary>
/// 	An attempt timed out and no attempts remain.
/// </summary>
/// <param name="attempts">
/// 	The number of attempts made.
/// </param>
public sealed class ApiTimeoutException(int attempts)
	: TypeSafeException($"The TypeSafe API timed out after {attempts} attempt(s).", attempts);
