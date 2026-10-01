# TypeSafe SDK for .NET

C# client for the TypeSafe API.
It follows the official [JavaScript](https://github.com/typesafe-ai/typesafe-sdk-js) and [Python](https://github.com/typesafe-ai/typesafe-sdk-python) SDKs.

## Requirements

- .NET 10 SDK
- A TypeSafe API key in the `TYPESAFE_API_KEY` environment variable

## Quick start

```csharp
using TypeSafe.Sdk;

using (TypeSafeClient client = new())
{
  SystemOneResponse response = await client.SystemOneAsync(
    "I was charged twice. Please fix this ASAP.",
    new Dictionary<string, Question>
      {
        ["category"] = Question.Choice("What is this ticket about?", new Dictionary<string, Entry>
          {
            ["billing"] = Entry.Null,
            ["technical"] = Entry.Null,
            ["other"] = Entry.Null
          })
      });

  Console.WriteLine(response.Choices["category"].Choice);
}
```

## Documentation

- [Usage guide](Docs/USAGE.md): questions, answers, configuration, retries, and errors.

## Development

```shell
dotnet restore TypeSafe.Sdk.slnx
dotnet format TypeSafe.Sdk.slnx --verify-no-changes --no-restore
dotnet build TypeSafe.Sdk.slnx -c Release --no-restore
dotnet test TypeSafe.Sdk.slnx -c Release --no-build
```

Open `TypeSafe.Sdk.slnx` in Visual Studio 2022 (17.13 or later) or Visual Studio 2026.
The console sample is in `Source/Samples/TypeSafe.Sdk.Console`.

## License

MIT. See [LICENSE](LICENSE).
