# PLAN.md вЂ” diceroller-building-blocks (Phase 0)

## Goal

Shared plumbing for the DiceRoller services, published as three versioned NuGet packages. No business logic, no service-specific types.

This phase must be finished before work starts in `diceroller-useraccess` or `diceroller-operative`.

## Repo setup

- [x] `DiceRoller.BuildingBlocks.slnx`
- [x] `global.json` pinned to the .NET 10 SDK
- [x] `Directory.Build.props`: `net10.0`, nullable, `TreatWarningsAsErrors`, `LangVersion latest`, `GenerateDocumentationFile`, SourceLink, MinVer, `Authors`, `RepositoryUrl`, `PackageReadmeFile`
- [x] `Directory.Packages.props` (Central Package Management)
- [x] `.editorconfig`, `.gitignore`, `README.md` (what each package is for, how to consume it)
- [x] Projects:
  - `src/DiceRoller.BuildingBlocks.Domain`
  - `src/DiceRoller.BuildingBlocks.Contracts`
  - `src/DiceRoller.BuildingBlocks.Web`
  - `tests/DiceRoller.BuildingBlocks.Tests`

## DiceRoller.BuildingBlocks.Domain (no package dependencies)

- [x] `Entity<TId>` with identity-based equality
- [x] `ValueObject` base (or guidance to use `record` types)
- [x] `ErrorType` enum: `Validation`, `Unauthorized`, `Forbidden`, `NotFound`, `Conflict`, `Unexpected`
- [x] `Error` record: `Code`, `Message`, `Type`, `Details` (`IReadOnlyDictionary<string, string[]>`, empty by default); factory methods `Error.Validation(...)`, `Error.NotFound(...)`, `Error.Conflict(...)`, `Error.Unauthorized(...)`, `Error.Forbidden(...)`, `Error.Unexpected(...)`
- [x] `Result` and `Result<T>`: `IsSuccess`, `IsFailure`, `Value` (throws if failure), `Error`; `Result.Success()`, `Result.Failure(error)`; implicit conversions from `T` and from `Error`; `Match` helper
- [x] `DomainException(Error)` вЂ” carries the `Error`, message = `Error.Message`
- [x] `Guard.Against(bool condition, Error error)` в†’ throws `DomainException`

## DiceRoller.BuildingBlocks.Contracts

- [x] `PagedResponse<T>`: `Items`, `Page`, `PageSize`, `TotalCount`, `TotalPages`
- [x] `PagedQuery`: `Page` (default 1), `PageSize` (default 10)
- [x] `PagedQueryValidator`: `Page` в‰Ґ 1, `PageSize` 1вЂ“100, with error codes
- [x] `JwtClaimNames` constants: `sub`, `email`, `given_name`, `family_name`, `jti`

## DiceRoller.BuildingBlocks.Web

Framework reference `Microsoft.AspNetCore.App`.

- [x] `ErrorMapper` вЂ” the only place an `Error` becomes HTTP:
  - status: Validation 400, Unauthorized 401, Forbidden 403, NotFound 404, Conflict 409, Unexpected 500
  - body: RFC 9457 ProblemDetails with `status`, `title`, `detail`, `errorCode`, `errors`, `traceId`
- [x] `ResultExtensions`: `ToActionResult()`, `ToCreatedResult(routeName, routeValues)` for `Result` / `Result<T>`
- [x] `GlobalExceptionHandler : IExceptionHandler`
  - `DomainException` в†’ `Result.Failure(ex.Error)` (logged at Warning)
  - `BadHttpRequestException` в†’ `Error.Validation`
  - anything else в†’ `Error.Unexpected` (logged at Error; exception details only in Development)
  - always written through `ErrorMapper`
- [x] `ValidationFilter` (action filter)
  - runs every registered `IValidator<T>` for action arguments
  - folds all failures into one `Error.Validation("Request.Invalid", ..., details)` where details = property в†’ messages
  - short-circuits through `ErrorMapper`
- [x] `ApiBehaviorOptions.SuppressModelStateInvalidFilter = true`; model-binding failures produce the same `Error.Validation`
- [x] `JwtOptions`: `Issuer`, `Audience`, `SigningKey` (в‰Ґ 32 bytes), `ExpiryMinutes`; validated with `ValidateOnStart()`
- [x] `AddJwtAuthentication(IConfiguration)`:
  - validate signature, issuer, audience, lifetime; `RequireSignedTokens`
  - `ValidAlgorithms = [HS256]`, `ClockSkew = 30s`, `MapInboundClaims = false`
  - 401 / 403 written through `ErrorMapper` (`JwtBearerEvents.OnChallenge` / `OnForbidden`)
  - fallback authorization policy = authenticated user
- [x] `AddServiceDefaults()` / `UseServiceDefaults()`:
  - ProblemDetails + `GlobalExceptionHandler`
  - controllers with `ValidationFilter` registered globally
  - OpenAPI (`Microsoft.AspNetCore.OpenApi`) + Scalar UI, with Bearer security scheme
  - health checks: `/health/live`, `/health/ready` (services add their DB check)
  - OpenTelemetry traces, metrics, logs; OTLP exporter enabled only when configured
  - `TimeProvider.System`, forwarded headers, correlation id (`X-Correlation-Id`) in logs and responses

## Tests

- [x] `Result` / `Result<T>` behaviour and implicit conversions
- [x] `Guard.Against` throws `DomainException` with the given error
- [x] `ErrorMapper`: every `ErrorType` в†’ correct status and body
- [x] `ValidationFilter`: multiple failures on several properties в†’ one 400 with all messages per field
- [x] `GlobalExceptionHandler`: a `DomainException` and a plain exception produce bodies with the same shape as a failed `Result`
- [x] Malformed JSON body в†’ same 400 shape (not ASP.NET Core's default)
- [x] `AddJwtAuthentication`: no token / expired / wrong key / wrong issuer / wrong audience в†’ 401 in the standard shape

## Packaging and CI

- [x] Versions from git tags (MinVer); no hardcoded `<Version>`
- [x] GitHub Actions `ci.yml`: restore в†’ build в†’ test on every PR and push to `main`
- [x] GitHub Actions `release.yml`: on tag `v*` в†’ `dotnet pack -c Release` в†’ push to GitHub Packages
- [x] Local feed for development: `dotnet pack -c Release -o ~/local-nuget`

## Done when

- [x] `dotnet build` with 0 warnings, `dotnet test` green
- [x] Three `.nupkg` files for v0.1.0 in `~/local-nuget` (or published to GitHub Packages)
- [x] README explains each package and how a service references it
