using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class Localize_Notifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "message",
                table: "notifications");

            migrationBuilder.AddColumn<string>(
                name: "language",
                table: "push_subscriptions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "language",
                table: "push_subscriptions");

            migrationBuilder.AddColumn<string>(
                name: "message",
                table: "notifications",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
