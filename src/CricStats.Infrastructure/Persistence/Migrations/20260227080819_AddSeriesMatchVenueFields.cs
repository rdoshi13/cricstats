using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CricStats.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSeriesMatchVenueFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VenueCountry",
                table: "SeriesMatches",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VenueName",
                table: "SeriesMatches",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VenueCountry",
                table: "SeriesMatches");

            migrationBuilder.DropColumn(
                name: "VenueName",
                table: "SeriesMatches");
        }
    }
}
