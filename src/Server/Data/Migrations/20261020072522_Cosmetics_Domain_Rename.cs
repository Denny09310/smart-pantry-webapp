using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class Cosmetics_Domain_Rename : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_notifications_items_pantry_item_id",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "nutri_score",
                table: "items");

            migrationBuilder.DropColumn(
                name: "nutrition_json",
                table: "items");

            migrationBuilder.DropColumn(
                name: "nutri_score",
                table: "cached_products");

            migrationBuilder.DropColumn(
                name: "nutrition_json",
                table: "cached_products");

            migrationBuilder.RenameColumn(
                name: "pantry_item_id",
                table: "notifications",
                newName: "cosmetic_item_id");

            migrationBuilder.RenameColumn(
                name: "expiration_date",
                table: "notifications",
                newName: "expiry_date");

            migrationBuilder.RenameIndex(
                name: "ix_notifications_pantry_item_id_expiration_date_type",
                table: "notifications",
                newName: "ix_notifications_cosmetic_item_id_expiry_date_type");

            migrationBuilder.RenameColumn(
                name: "location",
                table: "items",
                newName: "category");

            migrationBuilder.RenameColumn(
                name: "expiration_date",
                table: "items",
                newName: "expiry_date");

            migrationBuilder.RenameIndex(
                name: "ix_items_expiration_date",
                table: "items",
                newName: "ix_items_expiry_date");

            // Food-domain storage locations have no cosmetics meaning: park
            // pre-rename rows in the default "body" bucket so they pick it up
            // in the UI. Reversible in name only (Down restores the column).
            migrationBuilder.Sql("""
                UPDATE items
                SET category = 'body'
                WHERE category IN ('pantry', 'fridge', 'freezer', 'cabinet', 'cellar');
                """);

            migrationBuilder.AddForeignKey(
                name: "fk_notifications_items_cosmetic_item_id",
                table: "notifications",
                column: "cosmetic_item_id",
                principalTable: "items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_notifications_items_cosmetic_item_id",
                table: "notifications");

            migrationBuilder.RenameColumn(
                name: "expiry_date",
                table: "notifications",
                newName: "expiration_date");

            migrationBuilder.RenameColumn(
                name: "cosmetic_item_id",
                table: "notifications",
                newName: "pantry_item_id");

            migrationBuilder.RenameIndex(
                name: "ix_notifications_cosmetic_item_id_expiry_date_type",
                table: "notifications",
                newName: "ix_notifications_pantry_item_id_expiration_date_type");

            migrationBuilder.RenameColumn(
                name: "expiry_date",
                table: "items",
                newName: "expiration_date");

            migrationBuilder.RenameColumn(
                name: "category",
                table: "items",
                newName: "location");

            migrationBuilder.RenameIndex(
                name: "ix_items_expiry_date",
                table: "items",
                newName: "ix_items_expiration_date");

            migrationBuilder.AddColumn<string>(
                name: "nutri_score",
                table: "items",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nutrition_json",
                table: "items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nutri_score",
                table: "cached_products",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nutrition_json",
                table: "cached_products",
                type: "text",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_notifications_items_pantry_item_id",
                table: "notifications",
                column: "pantry_item_id",
                principalTable: "items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
