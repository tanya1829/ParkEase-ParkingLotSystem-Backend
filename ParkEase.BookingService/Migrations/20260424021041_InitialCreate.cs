using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ParkEase.BookingService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    BookingId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    LotId = table.Column<int>(type: "integer", nullable: false),
                    SpotId = table.Column<int>(type: "integer", nullable: false),
                    VehiclePlate = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    VehicleType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    BookingType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "PRE"),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CheckInTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CheckOutTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "RESERVED"),
                    TotalAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    PricePerHour = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.BookingId);
                });

            migrationBuilder.CreateTable(
                name: "OccupancyLogs",
                columns: table => new
                {
                    LogId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    LotId = table.Column<int>(type: "integer", nullable: false),
                    OccupiedSpots = table.Column<int>(type: "integer", nullable: false),
                    TotalSpots = table.Column<int>(type: "integer", nullable: false),
                    OccupancyRate = table.Column<double>(type: "double precision", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OccupancyLogs", x => x.LogId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_LotId",
                table: "Bookings",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_SpotId",
                table: "Bookings",
                column: "SpotId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Status",
                table: "Bookings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_UserId",
                table: "Bookings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_VehiclePlate",
                table: "Bookings",
                column: "VehiclePlate");

            migrationBuilder.CreateIndex(
                name: "IX_OccupancyLogs_LotId",
                table: "OccupancyLogs",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_OccupancyLogs_Timestamp",
                table: "OccupancyLogs",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "OccupancyLogs");
        }
    }
}
