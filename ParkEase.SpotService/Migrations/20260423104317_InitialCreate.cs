using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ParkEase.SpotService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParkingSpots",
                columns: table => new
                {
                    SpotId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    LotId = table.Column<int>(type: "integer", nullable: false),
                    SpotNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Floor = table.Column<int>(type: "integer", nullable: false),
                    SpotType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "STANDARD"),
                    VehicleType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "4W"),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "AVAILABLE"),
                    IsHandicapped = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsEVCharging = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    PricePerHour = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingSpots", x => x.SpotId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSpots_LotId",
                table: "ParkingSpots",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSpots_LotId_SpotNumber",
                table: "ParkingSpots",
                columns: new[] { "LotId", "SpotNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSpots_SpotType",
                table: "ParkingSpots",
                column: "SpotType");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSpots_Status",
                table: "ParkingSpots",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSpots_VehicleType",
                table: "ParkingSpots",
                column: "VehicleType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParkingSpots");
        }
    }
}
