# TypeSafe SDK for .NET - Repository Guide

## Purpose

This repository holds a .NET 10 client for the TypeSafe API (System One).
It follows the public API of the official TypeSafe SDKs for JavaScript (`@typesafe-ai/sdk`) and Python (`typesafe-sdk`).
When the official SDKs change, update the names, defaults, and wire format here to match.

## Repository layout

- `TypeSafe.Sdk.slnx` is the solution entry point.
- `Source/TypeSafe.Sdk` is the only library.
  - Root: `TypeSafeClient`, `ITypeSafeClient`, `Entry`, and `TypeSafeModels`.
  - `Questions`, `Answers`, `Exceptions`, and `Configuration` hold the public types.
  - `Internal` holds request serialization, response parsing, and failure classification.
- `Source/TypeSafe.Sdk.Tests` holds unit tests. Shared test infrastructure is in `Support`.
- `Source/Samples/TypeSafe.Sdk.Console` is a console sample.
- `Docs/USAGE.md` is the user documentation. Keep the root README short.

## SDK behavior

- One request sends one state and all questions to `POST {BaseUrl}/v1/systemone`.
- Preserve structured JSON values and explicit JSON nulls. Do not send object state as a JSON string.
- Preserve the values that the API reports. Do not infer missing usage, model, probabilities, or confidence.
- Model names pass through unchanged after basic validation.
- Serialize a request once and reuse the same bytes on every retry.
- The client owns authentication, retries, cancellation, response-size limits, and HTTP error mapping.
- Defaults match the official SDKs: 10 s attempt timeout, two retries, 500 ms initial backoff that doubles up to 5 s, 25 percent jitter, server delays capped at 60 s.
- Retry only HTTP 408, 429, and 5xx, transient connection failures, and timeouts.
- `ApiKey`, `BaseUrl`, and `DefaultModel` resolve in this order: option value, then `TYPESAFE_API_KEY` / `TYPESAFE_BASE_URL` / `TYPESAFE_DEFAULT_MODEL`, then the default. Do not read other environment variables.
- Tests must set `BaseUrl` and `DefaultModel` explicitly (see `Fixtures.Options`), so that the developer's environment cannot change the results.
- Keep the typed exception hierarchy. Keep exception messages free of bodies and secrets.

## Code conventions

- Target `net10.0`. Nullable reference types are on. Warnings are errors.
- Follow `.editorconfig`: tabs, no `var`, file-scoped namespaces, `_camelCase` private fields, braces on all blocks.
- All public types live in the `TypeSafe.Sdk` namespace, whatever the folder. The folders only organize files.
- Keep one declared type per file. Name the file after the type. Do not nest declared types.
- Use explicit `Main` methods. Do not use top-level statements.
- Add XML documentation to all public members. Put each tag on its own line.
- Write all text in English. Use `-` instead of an em dash and `ex.` instead of `e.g.`.
- Add package versions only in `Directory.Packages.props`.

## Security and privacy

- Never commit, print, log, or document real API keys or sensitive payloads.
- Do not log request bodies, response bodies, or authentication headers.
- The client creates its own `HttpClient` without redirects. Keep it that way.
- Keep redaction of the API key and default header values in error details.

## Tests and validation

- Use the deterministic `StubHandler`. Do not call the real API in tests.
- Cover request format, response parsing, validation, retries, cancellation, ownership, and exception mapping when you change those areas.
- Run these checks before you finish a change:

```shell
dotnet restore TypeSafe.Sdk.slnx
dotnet format TypeSafe.Sdk.slnx --verify-no-changes --no-restore
dotnet build TypeSafe.Sdk.slnx -c Release --no-restore
dotnet test TypeSafe.Sdk.slnx -c Release --no-build
```
