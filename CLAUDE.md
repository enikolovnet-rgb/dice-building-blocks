# CLAUDE.md — diceroller-building-blocks

## What this repo is

Shared **plumbing** for the DiceRoller microservices, shipped as versioned NuGet packages. It contains no business logic and no domain types of any service.

| Repo | Role |
| --- | --- |
| `diceroller-building-blocks` | **This repo** — `DiceRoller.BuildingBlocks.*` packages |
| `diceroller-useraccess` | Users + token issuing |
| `diceroller-operative` | Dice rolls + history |
| `diceroller-platform` | YARP gateway, system docker-compose, end-to-end tests |

The current task is in `PLAN.md`. Do only the phase you are asked to do.

## Commands

```bash
dotnet build                                   # must finish with 0 warnings
dotnet test
dotnet pack -c Release -o ~/local-nuget        # local feed the service repos read from
git tag v0.1.0 && git push --tags              # CI packs and publishes to GitHub Packages (only when asked)
```

## Packages and what may go in them

| Package | Contents | Allowed dependencies |
| --- | --- | --- |
| `DiceRoller.BuildingBlocks.Domain` | `Entity<TId>`, `ValueObject`, `Result`, `Result<T>`, `Error`, `ErrorType`, `DomainException`, `Guard` | **none** (BCL only) |
| `DiceRoller.BuildingBlocks.Contracts` | `PagedResponse<T>`, `PagedQuery` + validator, JWT claim-name constants | Domain, FluentValidation |
| `DiceRoller.BuildingBlocks.Web` | `ErrorMapper`, `ResultExtensions`, `GlobalExceptionHandler`, `ValidationFilter`, `JwtOptions`, `AddJwtAuthentication`, `AddServiceDefaults` | Domain, Contracts, `Microsoft.AspNetCore.App` framework reference, JwtBearer, FluentValidation, OpenTelemetry, Scalar |

## Rules specific to this repo

- **Nothing service-specific.** No `User`, `DiceRoll`, error codes of a service, routes or DTOs of an endpoint. If something is used by only one service, it belongs in that service.
- **The Domain package stays dependency-free**, so services' Domain projects never pull in ASP.NET Core.
- **Public API is a contract.** Every public type and member has XML docs. Removing or changing a public member is a breaking change → major version bump. Prefer adding over changing.
- Versioning comes from git tags via MinVer; never hardcode `<Version>`.
- `ErrorMapper` is the only place that turns an `Error` into an HTTP response. `GlobalExceptionHandler` and `ValidationFilter` must both go through it, so every failure has the same body.
- `ErrorType` → status: Validation 400, Unauthorized 401, Forbidden 403, NotFound 404, Conflict 409, Unexpected 500.
- `GlobalExceptionHandler`: `DomainException` → `Result.Failure(ex.Error)`; `BadHttpRequestException` → `Error.Validation`; anything else → `Error.Unexpected`, logged, with exception details only in Development.
- `ValidationFilter`: runs every registered `IValidator<T>` for action arguments, folds all failures into one `Error.Validation` with `Details` (property → messages); `SuppressModelStateInvalidFilter = true` so ASP.NET Core's default 400 never appears.
- `AddJwtAuthentication`: validate signature, issuer, audience, lifetime; `ValidAlgorithms = [HS256]`; `ClockSkew = 30s`; `MapInboundClaims = false`; 401/403 written through `ErrorMapper`.
- Tests prove the contract: a failed `Result`, a `DomainException`, a validation failure and an unexpected exception all produce bodies with the same shape.

<!-- ===== Everything below is identical in every DiceRoller repo ===== -->

## Error handling — what the packages must support

| Failure | How it is raised | Who turns it into HTTP |
| --- | --- | --- |
| Invalid input | FluentValidation validator | `ValidationFilter` → `Error.Validation` with per-field `Details` |
| Business rule | handler returns `Result.Failure(error)` | controller via `ToActionResult()` |
| Domain invariant broken | `throw new DomainException(error)` | `GlobalExceptionHandler` → `Result.Failure(ex.Error)` |
| Anything unexpected | any other exception | `GlobalExceptionHandler` → `Error.Unexpected` (500) |

Every error body: RFC 9457 with `status`, `title`, `detail`, `errorCode`, `errors`, `traceId`.

## Security and configuration

- No secrets in this repo, including test keys used outside test projects.
- Options classes are validated with `ValidateOnStart()` so bad config fails at startup in every service.

## Testing

- xUnit v3, Moq, Shouldly. Name tests `Method_Scenario_ExpectedResult`. Every bug fix gets a test that fails without the fix.
- Web components are tested through a minimal test host (`WebApplicationFactory` or `TestServer`), asserting the actual HTTP body.

## Code style

- .NET 10, C# latest, nullable enabled, file-scoped namespaces, primary constructors where they read well, `sealed` by default (except deliberate base classes).
- Central Package Management: versions only in `Directory.Packages.props`.
- No commented-out code, no `TODO` without a matching PLAN.md item.

## How to work

1. Read `PLAN.md` and restate the phase's tasks before writing code. In plan mode, propose the public API and wait for approval.
2. Work in small steps. After each step run `dotnet build` and `dotnet test`; fix failures before moving on.
3. Tick the matching checkbox in `PLAN.md` when a task is done.
4. Do not add features or NuGet packages that `PLAN.md` doesn't list — ask first.
5. Do not change anything in another repo.
6. Do not commit, tag or push unless asked. When asked, use Conventional Commits (`feat:`, `fix:`, `test:`, `chore:`); a breaking change uses `feat!:`.
7. Finish by checking the phase's "Done when" list item by item and reporting anything not met.
