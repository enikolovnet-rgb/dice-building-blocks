# DiceRoller.BuildingBlocks

Shared plumbing for the DiceRoller microservices, shipped as versioned NuGet packages.
These packages contain no business logic and no service-specific types.

| Package | What it is for | Dependencies |
| --- | --- | --- |
| `DiceRoller.BuildingBlocks.Domain` | DDD building blocks: `Entity<TId>`, `ValueObject`, `Result`, `Result<T>`, `Error`, `ErrorType`, `DomainException`, `Guard` | none (BCL only) |
| `DiceRoller.BuildingBlocks.Contracts` | API contracts shared by every service: `PagedResponse<T>`, `PagedQuery` + `PagedQueryValidator`, `JwtClaimNames` | FluentValidation |
| `DiceRoller.BuildingBlocks.Web` | ASP.NET Core plumbing: error mapping, exception handling, validation filter, JWT authentication, service defaults | Domain, Contracts, ASP.NET Core, JwtBearer, OpenAPI, Scalar, OpenTelemetry |

Reference the package that matches the layer: a service's Domain project references only
`DiceRoller.BuildingBlocks.Domain`, so it never pulls in ASP.NET Core.

## Domain

### Errors and results

An `Error` has a stable `Code`, a human-readable `Message`, an `ErrorType` and optional per-field `Details`.
The `ErrorType` decides the HTTP status in the web layer:
`Validation` 400, `Unauthorized` 401, `Forbidden` 403, `NotFound` 404, `Conflict` 409, `Unexpected` 500.

Return a `Result` / `Result<T>` for expected business failures. Values and errors convert implicitly:

```csharp
public Result<DiceRoll> Roll(int sides)
{
    if (sides < 2)
    {
        return Error.Validation("Roll.InvalidSides", "A die needs at least two sides.");
    }

    return new DiceRoll(sides);
}

var message = Roll(6).Match(roll => $"Rolled {roll.Value}", error => error.Message);
```

`Value` throws on a failed result and `Error` throws on a successful one, so check `IsSuccess` / `IsFailure`
or use `Match`. Implicit conversion does not work when `T` is an interface; use `Result.Success<T>(value)`.

### Invariants

Throw a `DomainException` when an invariant is broken. `Guard.Against` is the short form:

```csharp
Guard.Against(sides < 2, Error.Validation("Roll.InvalidSides", "A die needs at least two sides."));
```

The web layer turns a `DomainException` into the same response as a failed `Result`.

### Entities and value objects

Derive entities from `Entity<TId>`; equality is by type and `Id`. For value objects prefer a `sealed record`;
derive from `ValueObject` only when equality needs custom components.

## Contracts

- `PagedQuery` — `Page` (default 1) and `PageSize` (default 10, max 100) for list requests.
- `PagedQueryValidator` — reports `Paging.InvalidPage` and `Paging.InvalidPageSize`.
- `PagedResponse<T>` — `Items`, `Page`, `PageSize`, `TotalCount`, `TotalPages`.
- `JwtClaimNames` — `sub`, `email`, `given_name`, `family_name`, `jti`.

## Web

### Wiring a service

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddScoped<IValidator<CreateRollRequest>, CreateRollRequestValidator>();
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("db", tags: [ServiceDefaultsExtensions.ReadyTag]);

var app = builder.Build();
app.UseServiceDefaults();
app.Run();
```

`AddServiceDefaults()` registers:

- ProblemDetails and `GlobalExceptionHandler`;
- controllers with `ValidationFilter` as a global filter, and ASP.NET Core's automatic 400 turned off;
- OpenAPI with a Bearer security scheme;
- health checks;
- OpenTelemetry traces, metrics and logs, exported over OTLP only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set;
- `TimeProvider.System`;
- forwarded headers (`X-Forwarded-For`, `X-Forwarded-Proto`). Every proxy is trusted, so expose services only
  behind the gateway.

`UseServiceDefaults()` adds, in order:

1. forwarded headers;
2. the `X-Correlation-Id` header: echoed back on the response, created when missing, and added to the log scope;
3. the exception handler;
4. authentication and authorization;
5. OpenAPI at `/openapi/v1.json` and Scalar at `/scalar` (Development only);
6. `/health/live` (no checks) and `/health/ready` (checks tagged `ready`);
7. controllers.

The health and documentation endpoints allow anonymous access.

### Errors

`ErrorMapper` is the only place an `Error` becomes HTTP. Every failure has the same RFC 9457 body
(`application/problem+json`):

```json
{
  "status": 400,
  "title": "Bad Request",
  "detail": "One or more validation errors occurred.",
  "errorCode": "Request.Invalid",
  "errors": { "Name": ["Name is required."] },
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

| Failure | Raised by | Response |
| --- | --- | --- |
| Invalid input | an `IValidator<T>` for an action argument, or a model-binding failure such as malformed JSON | 400 `Request.Invalid` with per-field `errors` |
| Business rule | a handler returns `Result.Failure(error)`; the controller returns `result.ToActionResult()` | status from `ErrorType` |
| Broken invariant | `throw new DomainException(error)` / `Guard.Against(...)` | same as a failed `Result` |
| Unreadable request | `BadHttpRequestException` | 400 `Request.Malformed` |
| Anything else | any other exception | 500 `General.Unexpected`, logged; exception details only in Development |
| No / bad token | JWT bearer challenge | 401 `Auth.Unauthorized`, `Auth.TokenExpired` or `Auth.InvalidToken` |
| Not allowed | authorization | 403 `Auth.Forbidden` |

In controllers:

```csharp
[HttpGet("{id}")]
public async Task<IActionResult> Get(Guid id) => (await handler.Handle(id)).ToActionResult();       // 200 / error

[HttpPost]
public async Task<IActionResult> Create(CreateRollRequest request) =>
    (await handler.Handle(request)).ToCreatedResult("GetRoll", new { id = ... });                  // 201 / error
```

`Result.ToActionResult()` returns 204 on success. `Result<T>.ToActionResult()` returns 200 with the value.

### Authentication

`AddJwtAuthentication(configuration)` binds the `Jwt` section to `JwtOptions` and validates it at startup:

```json
{ "Jwt": { "Issuer": "diceroller", "Audience": "diceroller", "SigningKey": "<from secrets>", "ExpiryMinutes": 60 } }
```

`SigningKey` must be at least 32 bytes. Keep it in user secrets or environment variables, never in a repo.

Tokens must pass signature, issuer, audience and lifetime checks. Only HS256 is accepted, with a 30-second clock
skew. Inbound claims are not mapped, so read them with the `JwtClaimNames` constants. The fallback policy requires
an authenticated user, so mark public endpoints with `[AllowAnonymous]`.

## Consuming the packages

### Local feed (development)

```bash
dotnet pack -c Release -o ~/local-nuget
```

Add the feed to the service repo's `nuget.config`:

```xml
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="local" value="%USERPROFILE%/local-nuget" />
  </packageSources>
</configuration>
```

On Linux/macOS use `~/local-nuget` (expanded to an absolute path) as the value.

### GitHub Packages

Tagged releases (`v*`) are published to GitHub Packages:

```xml
<add key="github" value="https://nuget.pkg.github.com/enikolovnet-rgb/index.json" />
```

Authenticate with a personal access token that has `read:packages`.

### Referencing

With Central Package Management in the service repo:

```xml
<!-- Directory.Packages.props -->
<PackageVersion Include="DiceRoller.BuildingBlocks.Domain" Version="0.1.0" />

<!-- the project -->
<PackageReference Include="DiceRoller.BuildingBlocks.Domain" />
```

## Developing

```bash
dotnet build   # must finish with 0 warnings
dotnet test
```

Versions come from git tags via MinVer (`v0.1.0` → `0.1.0`). Changing or removing a public member is a breaking
change and needs a major version bump.
