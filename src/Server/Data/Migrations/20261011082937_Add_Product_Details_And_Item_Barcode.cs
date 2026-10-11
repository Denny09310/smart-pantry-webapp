using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_Product_Details_And_Item_Barcode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "barcode",
                table: "items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "carbohydrates100g",
                table: "barcode_products",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "energy_kcal100g",
                table: "barcode_products",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "fat100g",
                table: "barcode_products",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "fiber100g",
                table: "barcode_products",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ingredients_text",
                table: "barcode_products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nutri_score_grade",
                table: "barcode_products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "proteins100g",
                table: "barcode_products",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "salt100g",
                table: "barcode_products",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "saturated_fat100g",
                table: "barcode_products",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "sugars100g",
                table: "barcode_products",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "barcode",
                table: "items");

            migrationBuilder.DropColumn(
                name: "carbohydrates100g",
                table: "barcode_products");

            migrationBuilder.DropColumn(
                name: "energy_kcal100g",
                table: "barcode_products");

            migrationBuilder.DropColumn(
                name: "fat100g",
                table: "barcode_products");

            migrationBuilder.DropColumn(
                name: "fiber100g",
                table: "barcode_products");

            migrationBuilder.DropColumn(
                name: "ingredients_text",
                table: "barcode_products");

            migrationBuilder.DropColumn(
                name: "nutri_score_grade",
                table: "barcode_products");

            migrationBuilder.DropColumn(
                name: "proteins100g",
                table: "barcode_products");

            migrationBuilder.DropColumn(
                name: "salt100g",
                table: "barcode_products");

            migrationBuilder.DropColumn(
                name: "saturated_fat100g",
                table: "barcode_products");

            migrationBuilder.DropColumn(
                name: "sugars100g",
                table: "barcode_products");
        }
    }
}
