namespace TypeSafe.Sdk;

/// <summary>
/// 	Retry behavior for a complete request.
/// 	The client retries HTTP 408, 429, and 5xx responses, connection failures, and timeouts.
/// </summary>
public sealed class RetryOptions
{
	/// <summary>
	/// 	Gets or sets the number of retries after the first attempt.
	/// 	Zero disables retries.
	/// </summary>
	public int MaxRetries { get; set; } = 2;

	/// <summary>
	/// 	Gets or sets the delay before the first retry.
	/// 	Each later retry doubles the delay.
	/// </summary>
	public TimeSpan InitialBackoff { get; set; } = TimeSpan.FromMilliseconds(500);

	/// <summary>
	/// 	Gets or sets the longest backoff delay between two attempts.
	/// </summary>
	public TimeSpan MaxBackoff { get; set; } = TimeSpan.FromSeconds(5);

	/// <summary>
	/// 	Gets or sets the fraction of each backoff delay that the client removes at random, from zero to one.
	/// 	Zero gives a fixed delay.
	/// </summary>
	public double Jitter { get; set; } = 0.25;

	/// <summary>
	/// 	Gets or sets whether the <c>retry-after-ms</c> and <c>Retry-After</c> headers can lengthen the delay.
	/// </summary>
	public bool RespectRetryAfter { get; set; } = true;

	/// <summary>
	/// 	Gets or sets the longest delay that a server header can request.
	/// 	The client shortens a longer request to this value.
	/// </summary>
	public TimeSpan MaxRetryAfter { get; set; } = TimeSpan.FromSeconds(60);

	/// <summary>
	/// 	Gets or sets whether the client retries transient connection failures.
	/// </summary>
	public bool RetryConnectionErrors { get; set; } = true;

	/// <summary>
	/// 	Gets or sets whether the client retries attempts that time out.
	/// </summary>
	public bool RetryTimeouts { get; set; } = true;
}
