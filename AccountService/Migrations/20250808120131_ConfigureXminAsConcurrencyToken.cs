using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountService.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureXminAsConcurrencyToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_Date_Default",
                table: "Transactions");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Accounts",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Accounts");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Date_Default",
                table: "Transactions",
                column: "Timestamp");
        }
    }
}
