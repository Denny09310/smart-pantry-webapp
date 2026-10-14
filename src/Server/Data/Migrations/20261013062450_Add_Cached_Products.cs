using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_Cached_Products : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cached_products",
                columns: table => new
                {
                    barcode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    brand = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    quantity = table.Column<double>(type: "double precision", nullable: true),
                    unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    nutri_score = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    nutrition_json = table.Column<string>(type: "text", nullable: true),
                    image_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cached_products", x => x.barcode);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cached_products");
        }
    }
}