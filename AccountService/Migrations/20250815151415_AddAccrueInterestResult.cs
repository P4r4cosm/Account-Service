using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountService.Migrations
{
    /// <inheritdoc />
    public partial class AddAccrueInterestResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccrualResults",
                columns: table => new
                {
                    accrued_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    period_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    period_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccrualResults");
        }
    }
}
