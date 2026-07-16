# SportBook backend

ASP.NET Core Web API. See the repository root `README.md` for prerequisites, the full local setup
sequence (Docker, SQL login, `dotnet user-secrets`), and running tests.

## Project layout

- `src/SportBook.Api` - controllers, JWT auth, DI wiring, `Program.cs`.
- `src/SportBook.Application` - services (business logic), DTOs, mapping.
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
