# Usage guide

## Client

`TypeSafeClient` is thread-safe.
Create one client and reuse it.
Dispose it when you finish.

```csharp
// Reads TYPESAFE_API_KEY.
using (TypeSafeClient client = new())
{
}

// Explicit options.
TypeSafeClientOptions options = new()
{
	ApiKey = "your-api-key",
	DefaultModel = TypeSafeModels.Preview,
	Timeout = TimeSpan.FromSeconds(30)
};
using (TypeSafeClient client = new(options))
{
}
```

| Option | Default | Meaning |
| --- | --- | --- |
| `ApiKey` | `TYPESAFE_API_KEY` | API key. Printable ASCII without spaces. |
| `BaseUrl` | `TYPESAFE_BASE_URL`, then `https://api.typesafe.ai` | API root. The client calls `POST {BaseUrl}/v1/systemone`. |
| `DefaultModel` | `TYPESAFE_DEFAULT_MODEL`, then `jev-latest` | Model for requests that set none. |
| `Timeout` | 10 seconds | Timeout of each attempt. |
| `Retry` | see below | Retry settings. |
| `DefaultHeaders` | none | Extra headers for each request. |
| `MaxResponseBytes` | 8 MiB | Largest accepted response. |
| `TimeProvider` | system | Time source for timeouts and delays. |

An option value comes first.
When an option is null, the client reads the environment variable, then uses the default.

Pass your own `HttpClient` as the second constructor argument to share handlers, ex. with `IHttpClientFactory`.
The client does not dispose an `HttpClient` that you pass.
Disable redirects on that `HttpClient`, because the client sends your API key.

## Questions

All questions in one request answer the same state.
Question names are case-sensitive.

| Method | Answer | Rules |
| --- | --- | --- |
| `Question.Choice(instructions, criteria)` | `ChoiceAnswer` | 1 to 255 named options. |
| `Question.Score(instructions, criteria)` | `ScoreAnswer` | 2 to 10 ordered levels, indexed from zero. |
| `Question.Noul(instructions, criteria?)` | `NoulAnswer` | Yes/no. Optional `NoulCriteria` for yes and no. |

State and criteria are `Entry` values.
An `Entry` is a string, a JSON object, or a JSON array.
A string converts to an `Entry` implicitly.
Use `Entry.Null` for an explicit JSON null, and `Entry.FromObject(...)` for structured data.
The client sends structured values as JSON, not as JSON strings.

```csharp
SystemOneRequest request = new(
	Entry.FromObject(new { ticket = "Charged twice", amounts = new[] { 49, 49 } }),
	new Dictionary<string, Question>
	{
		["urgency"] = Question.Score("How urgent is this?", ["Routine", "Soon", "Immediately"]),
		["refund"] = Question.Noul("Does the customer ask for a refund?")
	})
{
	Model = TypeSafeModels.Jev1_13_0
};
SystemOneResponse response = await client.SystemOneAsync(request);
```

## Answers

| Member | Type | Meaning |
| --- | --- | --- |
| `response.Choices[name]` | `ChoiceAnswer` | `Choice`, `Confidence`, `Probabilities`. |
| `response.Scores[name]` | `ScoreAnswer` | `Score` (can be fractional), `Confidence`, `Legend`, `Probabilities`. |
| `response.Nouls[name]` | `NoulAnswer` | `Noul`, the probability of yes from 0 to 1. |
| `response.Answers` | `Answer` map | All answers. `GetAnswer<T>(name)` checks the type. |
| `response.Model` | `string` | Model that answered. |
| `response.Usage` | `Usage` | `InputTokens` and `OutputTokens`. |
| `response.RequestId` | `string?` | Request ID from `x-typesafe-request-id` (or `x-request-id`). |

The SDK reports the values that the API sends.
It applies no threshold to a `Noul` value.
Fields that the SDK does not model stay in `AdditionalData`.
The client validates each answer against its question.
A mismatch throws `InvalidResponseException`.

## Retries

The client retries these failures:

- HTTP 408, 429, and all 5xx statuses
- Transient connection failures
- Attempt timeouts

The client does not retry authentication, permission, or validation errors.
It does not retry invalid responses or caller cancellation.

| `RetryOptions` member | Default | Meaning |
| --- | --- | --- |
| `MaxRetries` | 2 | Retries after the first attempt. Zero disables retries. |
| `InitialBackoff` | 500 ms | Delay before the first retry. Each retry doubles it. |
| `MaxBackoff` | 5 s | Longest backoff delay between attempts. |
| `Jitter` | 0.25 | Fraction of each backoff delay removed at random. |
| `RespectRetryAfter` | true | A `retry-after-ms` or `Retry-After` header can lengthen the delay. `retry-after-ms` comes first. |
| `MaxRetryAfter` | 60 s | The client shortens a longer server delay to this value. |
| `RetryConnectionErrors` | true | Retry transient connection failures. |
| `RetryTimeouts` | true | Retry attempts that time out. |

Every request carries an `X-TypeSafe-Retry-Count` header: 0 on the first attempt, then 1, 2, and so on.
Pass a `CancellationToken` to stop requests and waits.

## Errors

| Exception | Cause |
| --- | --- |
| `TypeSafeException` | Base type. Also thrown when no API key is set. |
| `ApiException` | Non-success HTTP response. Has `StatusCode`, `RequestId`, `Details`, and `RetryAfter`. |
| `ApiAuthenticationException` | HTTP 401. |
| `ApiPermissionException` | HTTP 403. |
| `ApiValidationException` | HTTP 400 or 422. |
| `ApiRateLimitException` | HTTP 429. |
| `ApiConnectionException` | Network failure after all attempts. |
| `ApiTimeoutException` | Timeout after all attempts. |
| `InvalidResponseException` | A success response broke the contract. |
| `ArgumentException` | Invalid request. The client throws it before it sends anything. |
| `OperationCanceledException` | The caller canceled the request. |

Exception messages never contain request or response bodies.
`ApiException.Details` can contain application data.
The client removes the API key from it.
It also removes default header values of 8 characters or more.
