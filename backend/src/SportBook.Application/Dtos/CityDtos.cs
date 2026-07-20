namespace SportBook.Application.Dtos;

/// <summary>
/// Explicit response whitelist (contracts/api.md Cities section) - `Population` is deliberately
/// not exposed; ranking on it is the server's job, not the client's.
/// </summary>
public record CityResponse(
    int Id,
    string NameEn,
    string NameUk,
    string NamePt,
    string NameEs,
    string RegionEn,
    string RegionUk,
    string RegionPt,
    string RegionEs,
    decimal Latitude,
    decimal Longitude);
