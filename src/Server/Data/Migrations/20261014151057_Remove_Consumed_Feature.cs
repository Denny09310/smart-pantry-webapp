using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class Remove_Consumed_Feature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Consumed items are deleted outright under the new semantics:
            // drop their rows before dropping the columns that flagged them.
            migrationBuilder.Sql("""
                DELETE FROM items WHERE consumed_at IS NOT NULL
                """);

            migrationBuilder.DropColumn(
                name: "consumed_at",
                table: "items");

            migrationBuilder.DropColumn(
                name: "consumed_by_member_id",
                table: "items");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "consumed_at",
                table: "items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "consumed_by_member_id",
                table: "items",
                type: "text",
                nullable: true);
        }
    }
}
