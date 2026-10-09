using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_Members : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "created_by_member_id",
                table: "items",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "members",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    color = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_members", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_items_created_by_member_id",
                table: "items",
                column: "created_by_member_id");

            migrationBuilder.AddForeignKey(
                name: "fk_items_members_created_by_member_id",
                table: "items",
                column: "created_by_member_id",
                principalTable: "members",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_items_members_created_by_member_id",
                table: "items");

            migrationBuilder.DropTable(
                name: "members");

            migrationBuilder.DropIndex(
                name: "ix_items_created_by_member_id",
                table: "items");

            migrationBuilder.DropColumn(
                name: "created_by_member_id",
                table: "items");
        }
    }
}
