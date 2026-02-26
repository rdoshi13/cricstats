using System;
using CricStats.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CricStats.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CricStatsDbContext))]
[Migration("20260222000000_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Teams",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                ShortName = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                SourceProvider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                ExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                LastSyncedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Teams", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Venues",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                Longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                SourceProvider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                ExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                LastSyncedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Venues", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Matches",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Format = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                StartTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                VenueId = table.Column<Guid>(type: "uuid", nullable: false),
                HomeTeamId = table.Column<Guid>(type: "uuid", nullable: false),
                AwayTeamId = table.Column<Guid>(type: "uuid", nullable: false),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                SourceProvider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                ExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                LastSyncedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Matches", x => x.Id);
                table.ForeignKey(
                    name: "FK_Matches_Teams_AwayTeamId",
                    column: x => x.AwayTeamId,
                    principalTable: "Teams",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Matches_Teams_HomeTeamId",
                    column: x => x.HomeTeamId,
                    principalTable: "Teams",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Matches_Venues_VenueId",
                    column: x => x.VenueId,
                    principalTable: "Venues",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "WeatherSnapshots",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                VenueId = table.Column<Guid>(type: "uuid", nullable: false),
                TimestampUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Temperature = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                Humidity = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                WindSpeed = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                PrecipProbability = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                PrecipAmount = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                SourceProvider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                ExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                LastSyncedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WeatherSnapshots", x => x.Id);
                table.ForeignKey(
                    name: "FK_WeatherSnapshots_Venues_VenueId",
                    column: x => x.VenueId,
                    principalTable: "Venues",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "InningsScores",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                InningsNo = table.Column<int>(type: "integer", nullable: false),
                Runs = table.Column<int>(type: "integer", nullable: false),
                Wickets = table.Column<int>(type: "integer", nullable: false),
                Overs = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                SourceProvider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                ExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                LastSyncedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InningsScores", x => x.Id);
                table.ForeignKey(
                    name: "FK_InningsScores_Matches_MatchId",
                    column: x => x.MatchId,
                    principalTable: "Matches",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_InningsScores_Teams_TeamId",
                    column: x => x.TeamId,
                    principalTable: "Teams",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "MatchWeatherRisks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                CompositeRiskScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                RiskLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                ComputedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MatchWeatherRisks", x => x.Id);
                table.ForeignKey(
                    name: "FK_MatchWeatherRisks_Matches_MatchId",
                    column: x => x.MatchId,
                    principalTable: "Matches",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_InningsScores_MatchId_TeamId_InningsNo",
            table: "InningsScores",
            columns: new[] { "MatchId", "TeamId", "InningsNo" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_InningsScores_SourceProvider_ExternalId",
            table: "InningsScores",
            columns: new[] { "SourceProvider", "ExternalId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_InningsScores_TeamId",
            table: "InningsScores",
            column: "TeamId");

        migrationBuilder.CreateIndex(
            name: "IX_Matches_AwayTeamId",
            table: "Matches",
            column: "AwayTeamId");

        migrationBuilder.CreateIndex(
            name: "IX_Matches_Format_StartTimeUtc",
            table: "Matches",
            columns: new[] { "Format", "StartTimeUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_Matches_HomeTeamId",
            table: "Matches",
            column: "HomeTeamId");

        migrationBuilder.CreateIndex(
            name: "IX_Matches_SourceProvider_ExternalId",
            table: "Matches",
            columns: new[] { "SourceProvider", "ExternalId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Matches_StartTimeUtc",
            table: "Matches",
            column: "StartTimeUtc");

        migrationBuilder.CreateIndex(
            name: "IX_Matches_VenueId",
            table: "Matches",
            column: "VenueId");

        migrationBuilder.CreateIndex(
            name: "IX_MatchWeatherRisks_MatchId",
            table: "MatchWeatherRisks",
            column: "MatchId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Teams_Country",
            table: "Teams",
            column: "Country");

        migrationBuilder.CreateIndex(
            name: "IX_Teams_SourceProvider_ExternalId",
            table: "Teams",
            columns: new[] { "SourceProvider", "ExternalId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Venues_SourceProvider_ExternalId",
            table: "Venues",
            columns: new[] { "SourceProvider", "ExternalId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_WeatherSnapshots_SourceProvider_ExternalId",
            table: "WeatherSnapshots",
            columns: new[] { "SourceProvider", "ExternalId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_WeatherSnapshots_VenueId_TimestampUtc",
            table: "WeatherSnapshots",
            columns: new[] { "VenueId", "TimestampUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "InningsScores");

        migrationBuilder.DropTable(
            name: "MatchWeatherRisks");

        migrationBuilder.DropTable(
            name: "WeatherSnapshots");

        migrationBuilder.DropTable(
            name: "Matches");

        migrationBuilder.DropTable(
            name: "Teams");

        migrationBuilder.DropTable(
            name: "Venues");
    }
}
