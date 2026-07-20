namespace SportBook.Domain.Entities;

/// <summary>
/// A settlement from the GeoNames-derived reference directory (data-model.md City). Reference
/// data: rows change only via migration, never at runtime - the whole table is small enough to
/// cache in memory indefinitely (see CityService).
/// </summary>
public class City
{
    /// <summary>Primary key = GeoNames `geonameid` - a natural, globally stable external key, not an identity column, so the seed stays deterministic and diffable across dataset refreshes.</summary>
    public int Id { get; set; }

    public required string NameEn { get; set; }

    public required string NameUk { get; set; }

    public required string NamePt { get; set; }

    public required string NameEs { get; set; }

    public required string CountryCode { get; set; }

    public required string RegionEn { get; set; }

    public required string RegionUk { get; set; }

    public required string RegionPt { get; set; }

    public required string RegionEs { get; set; }

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    /// <summary>Used only for suggestion ranking (population DESC) - never exposed in DTOs.</summary>
    public int Population { get; set; }
}
