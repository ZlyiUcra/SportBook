using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportBook.Infrastructure.Migrations
{
    /// <summary>
    /// Second of the 3-migration City chain (data-model.md Migration chain, match-or-fail
    /// strategy): adds `Venues.CityId`/`Latitude`/`Longitude`, backfills `CityId` by exact string
    /// match of the legacy `Venues.City` value against City name columns, then guards - if any
    /// row remains unmatched the transaction throws and rolls back, listing the unmatched values
    /// so the (few, dev-only) rows can be fixed by hand and the migration re-run. Only once every
    /// row has a valid `CityId` does the column become NOT NULL.
    /// </summary>
    public partial class AddVenueCityIdAndCoordinates : Migration
    {
        private const string BackfillAndGuardSql = """
            UPDATE v
            SET v.[CityId] = c.[Id]
            FROM [Venues] v
            INNER JOIN [Cities] c ON v.[City] = c.[NameEn] OR v.[City] = c.[NameUk]
            WHERE v.[CityId] IS NULL;

            IF EXISTS (SELECT 1 FROM [Venues] WHERE [CityId] IS NULL)
            BEGIN
                DECLARE @unmatched nvarchar(max) = (
                    SELECT STRING_AGG(CONVERT(nvarchar(max), [City]), ', ')
                    FROM (SELECT DISTINCT [City] FROM [Venues] WHERE [CityId] IS NULL) AS u
                );
                DECLARE @msg nvarchar(2048) = N'AddVenueCityIdAndCoordinates: the following Venues.City values matched no directory city by exact name (EN or UK) - fix them manually and re-run the migration: ' + LEFT(@unmatched, 1800);
                THROW 50001, @msg, 1;
            END
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "Venues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "Venues",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "Venues",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.Sql(BackfillAndGuardSql);

            migrationBuilder.AlterColumn<int>(
                name: "CityId",
                table: "Venues",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Venues_CityId",
                table: "Venues",
                column: "CityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Venues_Cities_CityId",
                table: "Venues",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Venues_Cities_CityId",
                table: "Venues");

            migrationBuilder.DropIndex(
                name: "IX_Venues_CityId",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Venues");
        }
    }
}
