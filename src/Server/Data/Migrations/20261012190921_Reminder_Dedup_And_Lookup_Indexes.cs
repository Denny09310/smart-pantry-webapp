using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class Reminder_Dedup_And_Lookup_Indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_notifications_pantry_item_id_expiration_date_type",
                table: "notifications");

            migrationBuilder.AlterColumn<string>(
                name: "nutri_score",
                table: "items",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "barcode",
                table: "items",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            // Collapse pre-existing duplicate reminders (same identity) so the
            // unique index below builds. Keeps one row per identity: no data lost.
            migrationBuilder.Sql("""
                DELETE FROM notifications a USING notifications b
                WHERE a.ctid > b.ctid
                  AND a.pantry_item_id = b.pantry_item_id
                  AND a.expiration_date = b.expiration_date
                  AND a.type = b.type
                """);

            migrationBuilder.CreateIndex(
                name: "ix_notifications_pantry_item_id_expiration_date_type",
                table: "notifications",
                columns: new[] { "pantry_item_id", "expiration_date", "type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_items_expiration_date",
                table: "items",
                column: "expiration_date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_notifications_pantry_item_id_expiration_date_type",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "ix_items_expiration_date",
                table: "items");

            migrationBuilder.AlterColumn<string>(
                name: "nutri_score",
                table: "items",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2)",
                oldMaxLength: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "barcode",
                table: "items",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_notifications_pantry_item_id_expiration_date_type",
                table: "notifications",
                columns: new[] { "pantry_item_id", "expiration_date", "type" });
        }
    }
}
