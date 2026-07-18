using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportBook.Infrastructure.Migrations
{
    /// <summary>
    /// First of the 3-migration City chain (data-model.md Migration chain): creates the `Cities`
    /// reference table and seeds it from the embedded `Data/cities.csv` (produced offline by
    /// scripts/convert-geonames-cities.ps1). Never `HasData` - see research.md Seeding mechanism.
    /// </summary>
    public partial class CreateAndSeedCities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NameUk = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NamePt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CountryCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RegionEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RegionUk = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RegionPt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    Population = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                });

            // Deterministic INSERT batches read from the embedded seed file at migration-run
            // time (single source of truth with the committed csv - no values baked into this
            // file), batched well under SQL Server's 1000-row VALUES-list limit per statement.
            foreach (var batch in CitySeedCsvReader.ReadEmbedded().Chunk(500))
            {
                migrationBuilder.Sql(BuildInsertSql(batch));
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cities");
        }

        private static string BuildInsertSql(IEnumerable<CitySeedRow> rows)
        {
            var sb = new StringBuilder();
            sb.Append("INSERT INTO [Cities] ([Id],[NameEn],[NameUk],[NamePt],[CountryCode],[RegionEn],[RegionUk],[RegionPt],[Latitude],[Longitude],[Population]) VALUES ");
            sb.AppendJoin(",", rows.Select(r =>
                $"({r.Id.ToString(CultureInfo.InvariantCulture)},{SqlString(r.NameEn)},{SqlString(r.NameUk)},{SqlString(r.NamePt)},{SqlString(r.CountryCode)},{SqlString(r.RegionEn)},{SqlString(r.RegionUk)},{SqlString(r.RegionPt)},{r.Latitude.ToString(CultureInfo.InvariantCulture)},{r.Longitude.ToString(CultureInfo.InvariantCulture)},{r.Population.ToString(CultureInfo.InvariantCulture)})"));
            return sb.ToString();
        }

        private static string SqlString(string value) => "N'" + value.Replace("'", "''") + "'";
    }
}
