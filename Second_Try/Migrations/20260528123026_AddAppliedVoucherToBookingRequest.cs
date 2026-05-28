using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Second_Try.Migrations
{
    /// <inheritdoc />
    public partial class AddAppliedVoucherToBookingRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AppliedVoucherId",
                table: "BookingRequests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_AppliedVoucherId",
                table: "BookingRequests",
                column: "AppliedVoucherId");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingRequests_Vouchers_AppliedVoucherId",
                table: "BookingRequests",
                column: "AppliedVoucherId",
                principalTable: "Vouchers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingRequests_Vouchers_AppliedVoucherId",
                table: "BookingRequests");

            migrationBuilder.DropIndex(
                name: "IX_BookingRequests_AppliedVoucherId",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "AppliedVoucherId",
                table: "BookingRequests");
        }
    }
}
