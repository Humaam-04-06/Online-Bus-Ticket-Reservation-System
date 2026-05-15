using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Second_Try.Migrations
{
    /// <inheritdoc />
    public partial class AddSeatSelectionToRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SelectedSeatNumbers",
                table: "BookingRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectedSeatNumbers",
                table: "BookingRequests");
        }
    }
}
