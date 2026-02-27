using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CricStats.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSeriesCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Series",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    StartDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EndDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SourceProvider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastSyncedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Series", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SeriesMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SeriesId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Format = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    StartTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    StatusText = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SourceProvider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastSyncedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeriesMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeriesMatches_Series_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "Series",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Series_SourceProvider_ExternalId",
                table: "Series",
                columns: new[] { "SourceProvider", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Series_StartDateUtc",
                table: "Series",
                column: "StartDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SeriesMatches_SeriesId_SourceProvider_ExternalId",
                table: "SeriesMatches",
                columns: new[] { "SeriesId", "SourceProvider", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeriesMatches_StartTimeUtc",
                table: "SeriesMatches",
                column: "StartTimeUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SeriesMatches");

            migrationBuilder.DropTable(
                name: "Series");
        }
    }
}
