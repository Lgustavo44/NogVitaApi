using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NogVita.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFoodPortionsAndSubstitutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "portion_grams",
                table: "tb_meal_food",
                type: "numeric(7,2)",
                precision: 7,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "portion_name",
                table: "tb_meal_food",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "portion_quantity",
                table: "tb_meal_food",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "substitute_for_id",
                table: "tb_meal_food",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tb_food_portion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    food_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    grams = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_food_portion", x => x.id);
                    table.ForeignKey(
                        name: "fk_tb_food_portion_tb_food_food_id",
                        column: x => x.food_id,
                        principalTable: "tb_food",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tb_meal_food_substitute_for_id",
                table: "tb_meal_food",
                column: "substitute_for_id");

            migrationBuilder.CreateIndex(
                name: "ux_food_portion_food_name",
                table: "tb_food_portion",
                columns: new[] { "food_id", "name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_tb_meal_food_tb_meal_food_substitute_for_id",
                table: "tb_meal_food",
                column: "substitute_for_id",
                principalTable: "tb_meal_food",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tb_meal_food_tb_meal_food_substitute_for_id",
                table: "tb_meal_food");

            migrationBuilder.DropTable(
                name: "tb_food_portion");

            migrationBuilder.DropIndex(
                name: "ix_tb_meal_food_substitute_for_id",
                table: "tb_meal_food");

            migrationBuilder.DropColumn(
                name: "portion_grams",
                table: "tb_meal_food");

            migrationBuilder.DropColumn(
                name: "portion_name",
                table: "tb_meal_food");

            migrationBuilder.DropColumn(
                name: "portion_quantity",
                table: "tb_meal_food");

            migrationBuilder.DropColumn(
                name: "substitute_for_id",
                table: "tb_meal_food");
        }
    }
}
