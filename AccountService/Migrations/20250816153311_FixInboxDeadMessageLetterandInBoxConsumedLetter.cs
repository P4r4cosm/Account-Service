using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AccountService.Migrations
{
    /// <inheritdoc />
    public partial class FixInboxDeadMessageLetterandInBoxConsumedLetter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_inbox_dead_letters",
                table: "inbox_dead_letters");

            migrationBuilder.DropIndex(
                name: "IX_inbox_dead_letters_MessageId",
                table: "inbox_dead_letters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_inbox_consumed_messages",
                table: "inbox_consumed_messages");

            migrationBuilder.DropIndex(
                name: "IX_inbox_consumed_messages_Id",
                table: "inbox_consumed_messages");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "inbox_dead_letters");

            migrationBuilder.DropColumn(
                name: "Exchange",
                table: "inbox_dead_letters");

            migrationBuilder.DropColumn(
                name: "RoutingKey",
                table: "inbox_dead_letters");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "inbox_consumed_messages",
                newName: "MessageId");

            migrationBuilder.AlterColumn<Guid>(
                name: "MessageId",
                table: "inbox_dead_letters",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_inbox_dead_letters",
                table: "inbox_dead_letters",
                column: "MessageId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_inbox_consumed_messages",
                table: "inbox_consumed_messages",
                column: "MessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_inbox_dead_letters",
                table: "inbox_dead_letters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_inbox_consumed_messages",
                table: "inbox_consumed_messages");

            migrationBuilder.RenameColumn(
                name: "MessageId",
                table: "inbox_consumed_messages",
                newName: "Id");

            migrationBuilder.AlterColumn<Guid>(
                name: "MessageId",
                table: "inbox_dead_letters",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<long>(
                name: "Id",
                table: "inbox_dead_letters",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<string>(
                name: "Exchange",
                table: "inbox_dead_letters",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoutingKey",
                table: "inbox_dead_letters",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_inbox_dead_letters",
                table: "inbox_dead_letters",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_inbox_consumed_messages",
                table: "inbox_consumed_messages",
                columns: new[] { "Id", "Handler" });

            migrationBuilder.CreateIndex(
                name: "IX_inbox_dead_letters_MessageId",
                table: "inbox_dead_letters",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_inbox_consumed_messages_Id",
                table: "inbox_consumed_messages",
                column: "Id");
        }
    }
}
