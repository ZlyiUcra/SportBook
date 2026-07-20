# Quickstart: Colocate single-use DTOs into their owning Features folders

End-to-end validation that the move changed nothing observable. Backend-only; no frontend changes
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

EXPECT: clean build (only the pre-existing `NU1510`/`NU1903` warnings); 51 unit tests green; 72
integration tests green - the same counts as spec 010, since this feature adds no new test and
removes none.

## Structural check

- `grep -c "record\|enum" backend/src/SportBook.Application/Dtos/*.cs` -> EXPECT: 7 shared DTO
  declarations total across the 5 `Dtos/*.cs` files (`UserResponse`, `AuthResponse`,
  `BookingResponse`, `CityResponse`, `CourtResponse`, `ReviewResponse`, `VenueDetailResponse`).
- For each of the 10 single-use DTOs in research.md's table: `grep -rn "record <Name>\|enum <Name>"
  backend/src/SportBook.Application/Features` -> EXPECT: found in exactly the destination file
  research.md's table names, and nowhere under `Dtos/`.

## Manual verification (live server)

```powershell
dotnet run --project backend/src/SportBook.Api
```

Then, against `http://localhost:5217`, spot-check one endpoint per moved DTO category (request
and response):

1. `POST /api/venues` (authenticated, owner) with a `CreateVenueRequest`-shaped body -> EXPECT
   `201` with a `VenueDetailResponse`-shaped body (moved request DTO, unmoved shared response DTO
   in the same round-trip).
2. `GET /api/venues?...` -> EXPECT `200` with a list of `VenueSummaryResponse`-shaped items (moved
   response DTO, still embedding the unmoved shared `CityResponse`).
