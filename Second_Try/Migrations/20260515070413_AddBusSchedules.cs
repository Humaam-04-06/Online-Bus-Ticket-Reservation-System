using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Second_Try.Migrations
{
    /// <inheritdoc />
    public partial class AddBusSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BusScheduleId",
                table: "BookingRequests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BusSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RouteId = table.Column<int>(type: "int", nullable: false),
                    BusType = table.Column<int>(type: "int", nullable: false),
                    DepartureTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    ArrivalTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusSchedules_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_BusScheduleId",
                table: "BookingRequests",
                column: "BusScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_BusSchedules_RouteId",
                table: "BusSchedules",
                column: "RouteId");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingRequests_BusSchedules_BusScheduleId",
                table: "BookingRequests",
                column: "BusScheduleId",
                principalTable: "BusSchedules",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingRequests_BusSchedules_BusScheduleId",
                table: "BookingRequests");

            migrationBuilder.DropTable(
                name: "BusSchedules");

            migrationBuilder.DropIndex(
                name: "IX_BookingRequests_BusScheduleId",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "BusScheduleId",
                table: "BookingRequests");
        }
    }
}
