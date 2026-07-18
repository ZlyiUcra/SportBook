using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportBook.Infrastructure.Migrations
{
    /// <summary>
    /// Third and last of the 3-migration City chain (data-model.md Migration chain): drops the
    /// legacy `Venues.City` string column now that `CityId` is populated and NOT NULL. Kept as
    /// its own migration - by the time this runs, `AddVenueCityIdAndCoordinates` has already
    /// proven every venue has a valid `CityId`, so nothing reads `City` anymore.
    /// </summary>
    public partial class DropVenueLegacyCity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                table: "Venues");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Venues",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
