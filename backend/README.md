# SportBook backend

ASP.NET Core Web API. See the repository root `README.md` for prerequisites, the full local setup
sequence (Docker, SQL login, `dotnet user-secrets`), and running tests.

## Project layout

- `src/SportBook.Api` - Minimal API endpoints (`Endpoints/`, one `MapXxxEndpoints` file per
  resource), JWT auth, DI wiring, `Program.cs`.
- `src/SportBook.Application` - vertical slices (`Features/<Resource>/<UseCase>/`, a
  Command/Query + Handler pair per action, dispatched via MediatR), a small `Services/` layer for
  logic genuinely shared across slices, DTOs, mapping.
- `src/SportBook.Domain` - entities and enums, no framework dependencies.
- `src/SportBook.Infrastructure` - EF Core `DbContext`, migrations, SQL Server provider.
- `tests/SportBook.UnitTests` - xUnit + EF Core Sqlite in-memory.
- `tests/SportBook.IntegrationTests` - xUnit + `WebApplicationFactory` against the real SQL
  Server container.

## Common commands

```powershell
dotnet build SportBook.sln
dotnet run --project src/SportBook.Api
dotnet ef migrations add <Name> --project src/SportBook.Infrastructure --startup-project src/SportBook.Api
dotnet ef database update --project src/SportBook.Infrastructure --startup-project src/SportBook.Api
dotnet test tests/SportBook.UnitTests/SportBook.UnitTests.csproj
dotnet test tests/SportBook.IntegrationTests/SportBook.IntegrationTests.csproj
```

## City reference data

`src/SportBook.Infrastructure/Data/cities.csv` is a committed, pre-converted GeoNames extract
(Ukrainian settlements, population >= 500) that the `CreateAndSeedCities` migration reads at
migration-run time - regular setup never needs to touch it, `dotnet ef database update` seeds it
automatically. Regenerate it only when refreshing the dataset (new country, updated population
figures) via the root `scripts/convert-geonames-cities.ps1` script - see that script's header
comment for the raw GeoNames files it expects and where to download them.
