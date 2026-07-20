# Quickstart: Switch the backend's dispatch mechanism to MediatR

End-to-end validation that the swap changed nothing observable. Backend-only; no frontend changes
to validate.

## Prerequisites

- SQL Server container running (`docker-compose up -d`, see repository root `README.md`).
- Backend buildable: `dotnet build SportBook.sln` from the repository root.

## Automated checks

```powershell
dotnet build SportBook.sln
dotnet test backend/tests/SportBook.UnitTests/SportBook.UnitTests.csproj
dotnet test backend/tests/SportBook.IntegrationTests/SportBook.IntegrationTests.csproj
```

EXPECT: clean build (only the pre-existing `NU1510`/`NU1903` warnings, unrelated to this feature);
51 unit tests green; 72 integration tests green - the same counts as spec 009, since this feature
adds no new test and removes none.

## Manual verification (live server)

```powershell
dotnet run --project backend/src/SportBook.Api
```

Then, against `http://localhost:5217`:

1. `POST /api/auth/login` with valid demo credentials -> EXPECT `200` with an access/refresh token
   pair, same response shape as before.
2. `POST /api/auth/logout` with a valid access token and that login's refresh token -> EXPECT
   `204 No Content` (the trickiest case: a void-command Handler, MediatR's non-generic
   `IRequestHandler<TCommand>`, no `Unit` type involved).

## Structural check

- `grep -rl "using Mediator;" backend/src` -> EXPECT: no matches (every file now uses `using
  MediatR;`).
- `grep -rn "Mediator\.Abstractions\|Mediator\.SourceGenerator" backend/src/**/*.csproj` ->
  EXPECT: no matches; both `.csproj` files reference `MediatR` 14.2.0 instead.
