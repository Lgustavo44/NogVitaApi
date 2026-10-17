using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NogVita.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNutritionPlanTarget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "target_carbohydrate_grams",
                table: "tb_nutrition_plan",
                type: "numeric(7,2)",
                precision: 7,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "target_energy_kcal",
                table: "tb_nutrition_plan",
                type: "numeric(7,2)",
                precision: 7,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "target_fat_grams",
                table: "tb_nutrition_plan",
                type: "numeric(7,2)",
                precision: 7,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "target_protein_grams",
                table: "tb_nutrition_plan",
                type: "numeric(7,2)",
                precision: 7,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "target_carbohydrate_grams",
                table: "tb_nutrition_plan");

            migrationBuilder.DropColumn(
                name: "target_energy_kcal",
                table: "tb_nutrition_plan");

            migrationBuilder.DropColumn(
                name: "target_fat_grams",
                table: "tb_nutrition_plan");

            migrationBuilder.DropColumn(
                name: "target_protein_grams",
                table: "tb_nutrition_plan");
        }
    }
}
