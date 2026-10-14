using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NogVita.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNutritionPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tb_nutrition_plan",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nutritionist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_nutrition_plan", x => x.id);
                    table.ForeignKey(
                        name: "fk_tb_nutrition_plan_users_nutritionist_id",
                        column: x => x.nutritionist_id,
                        principalTable: "tb_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tb_nutrition_plan_users_patient_id",
                        column: x => x.patient_id,
                        principalTable: "tb_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_nutrition_plan_nutritionist_patient",
                table: "tb_nutrition_plan",
                columns: new[] { "nutritionist_id", "patient_id" });

            migrationBuilder.CreateIndex(
                name: "ix_nutrition_plan_patient_status",
                table: "tb_nutrition_plan",
                columns: new[] { "patient_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_nutrition_plan_patient_active",
                table: "tb_nutrition_plan",
                column: "patient_id",
                unique: true,
                filter: "status = 'Active'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tb_nutrition_plan");
        }
    }
}
