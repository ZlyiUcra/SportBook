using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportBook.Infrastructure.Migrations
{
    /// <summary>
    /// Adds Spanish city/region names (data-model.md City) on top of the already-shipped
    /// CreateAndSeedCities migration. New columns are added with an empty-string default so the
    /// ADD COLUMN succeeds against existing rows, then immediately backfilled from the same
    /// embedded `Data/cities.csv` (now with trailing NameEs/RegionEs columns) that the original
    /// migration reads - single source of truth, no values baked into this file (same pattern as
    /// CreateAndSeedCities).
    /// </summary>
    public partial class AddCitySpanishNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NameEs",
                table: "Cities",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RegionEs",
                table: "Cities",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            foreach (var batch in CitySeedCsvReader.ReadEmbedded().Chunk(500))
            {
                migrationBuilder.Sql(BuildUpdateSql(batch));
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NameEs",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "RegionEs",
                table: "Cities");
        }

        private static string BuildUpdateSql(IEnumerable<CitySeedRow> rows)
        {
            var sb = new StringBuilder();
            sb.Append("UPDATE c SET c.[NameEs] = v.NameEs, c.[RegionEs] = v.RegionEs FROM [Cities] c JOIN (VALUES ");
            sb.AppendJoin(",", rows.Select(r =>
                $"({r.Id.ToString(CultureInfo.InvariantCulture)},{SqlString(r.NameEs)},{SqlString(r.RegionEs)})"));
            sb.Append(") AS v (Id, NameEs, RegionEs) ON c.[Id] = v.Id");
            return sb.ToString();
        }

        private static string SqlString(string value) => "N'" + value.Replace("'", "''") + "'";
    }
}
