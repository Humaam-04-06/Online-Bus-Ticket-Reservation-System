using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Second_Try.Migrations
{
    /// <inheritdoc />
    public partial class AddVoucherMinimumFare : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MinimumFareRequired",
                table: "Vouchers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinimumFareRequired",
                table: "Vouchers");
        }
    }
}
