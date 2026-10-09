using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_Query_Indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_notifications_read_at",
                table: "notifications",
                column: "read_at");

            migrationBuilder.CreateIndex(
                name: "ix_items_expiration_date",
                table: "items",
                column: "expiration_date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_notifications_read_at",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "ix_items_expiration_date",
                table: "items");
        }
    }
}
