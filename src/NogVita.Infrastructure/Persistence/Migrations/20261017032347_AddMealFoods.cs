using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NogVita.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMealFoods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tb_meal_food",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    meal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    food_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_in_grams = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    snapshot_brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    snapshot_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    snapshot_source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    snapshot_source_reference = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    snapshot_taken_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    snapshot_nutrients_carbohydrate = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: true),
                    snapshot_nutrients_energy_kcal = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: true),
                    snapshot_nutrients_fat = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: true),
                    snapshot_nutrients_fiber = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: true),
                    snapshot_nutrients_protein = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: true),
                    snapshot_nutrients_sodium_mg = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_meal_food", x => x.id);
                    table.ForeignKey(
                        name: "fk_tb_meal_food_tb_food_food_id",
                        column: x => x.food_id,
                        principalTable: "tb_food",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tb_meal_food_tb_meal_meal_id",
                        column: x => x.meal_id,
                        principalTable: "tb_meal",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_meal_food_food_id",
                table: "tb_meal_food",
                column: "food_id");

            migrationBuilder.CreateIndex(
                name: "ux_meal_food_meal_food",
                table: "tb_meal_food",
                columns: new[] { "meal_id", "food_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tb_meal_food");
        }
    }
}
