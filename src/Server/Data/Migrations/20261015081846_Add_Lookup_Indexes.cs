using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_Lookup_Indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_notifications_created_at",
                table: "notifications",
                column: "created_at",
                descending: new[] { true });

            migrationBuilder.CreateIndex(
                name: "ix_items_barcode",
                table: "items",
                column: "barcode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_notifications_created_at",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "ix_items_barcode",
                table: "items");
        }
    }
}
