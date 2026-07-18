using System.Globalization;

namespace SportBook.Infrastructure.Migrations;

/// <summary>One row of the seed dataset produced by scripts/convert-geonames-cities.ps1 (data-model.md City).</summary>
internal sealed record CitySeedRow(
    int Id, string NameEn, string NameUk, string NamePt, string CountryCode,
    string RegionEn, string RegionUk, string RegionPt, decimal Latitude, decimal Longitude, int Population);

/// <summary>
/// Minimal RFC4180 CSV reader for the embedded seed data file - no CSV library dependency
/// (plan.md Primary Dependencies: no new NuGet packages). Handles quoted fields with embedded
/// commas/escaped quotes, which is all `Export-Csv` (scripts/convert-geonames-cities.ps1) ever
/// produces. Used only by the CreateAndSeedCities migration, at migration-run time - never
/// `HasData` (research.md Seeding mechanism).
/// </summary>
internal static class CitySeedCsvReader
{
    public static IReadOnlyList<CitySeedRow> ReadEmbedded()
    {
        var assembly = typeof(CitySeedCsvReader).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .Single(n => n.EndsWith("Data.cities.csv", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);

        var rows = new List<CitySeedRow>();
        reader.ReadLine(); // header row - column order is fixed by ParseLine below
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Length == 0)
            {
                continue;
            }

            var f = ParseLine(line);
            rows.Add(new CitySeedRow(
                Id: int.Parse(f[0], CultureInfo.InvariantCulture),
                NameEn: f[1],
                NameUk: f[2],
                NamePt: f[3],
                CountryCode: f[4],
                RegionEn: f[5],
                RegionUk: f[6],
                RegionPt: f[7],
                Latitude: decimal.Parse(f[8], CultureInfo.InvariantCulture),
                Longitude: decimal.Parse(f[9], CultureInfo.InvariantCulture),
                Population: int.Parse(f[10], CultureInfo.InvariantCulture)));
        }

        return rows;
    }

    private static string[] ParseLine(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(ch);
                }
            }
            else if (ch == '"')
            {
                inQuotes = true;
            }
            else if (ch == ',')
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }

        fields.Add(current.ToString());
        return [.. fields];
    }
}
