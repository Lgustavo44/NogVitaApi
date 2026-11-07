using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NogVita.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFoodDiary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tb_diary_day",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    meal_check_ins = table.Column<string>(type: "jsonb", nullable: false),
                    extra_foods = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_diary_day", x => x.id);
                    table.ForeignKey(
                        name: "fk_tb_diary_day_users_patient_id",
                        column: x => x.patient_id,
                        principalTable: "tb_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_weekly_check_in",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nutritionist_id = table.Column<Guid>(type: "uuid", nullable: true),
                    week_start = table.Column<DateOnly>(type: "date", nullable: false),
                    hunger = table.Column<int>(type: "integer", nullable: false),
                    energy = table.Column<int>(type: "integer", nullable: false),
                    sleep = table.Column<int>(type: "integer", nullable: false),
                    difficulties = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    feedback = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    feedback_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_weekly_check_in", x => x.id);
                    table.ForeignKey(
                        name: "fk_tb_weekly_check_in_tb_user_nutritionist_id",
                        column: x => x.nutritionist_id,
                        principalTable: "tb_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tb_weekly_check_in_tb_user_patient_id",
                        column: x => x.patient_id,
                        principalTable: "tb_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_diary_day_patient_date",
                table: "tb_diary_day",
                columns: new[] { "patient_id", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_weekly_check_in_awaiting",
                table: "tb_weekly_check_in",
                columns: new[] { "nutritionist_id", "feedback" });

            migrationBuilder.CreateIndex(
                name: "ux_weekly_check_in_patient_week",
                table: "tb_weekly_check_in",
                columns: new[] { "patient_id", "week_start" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tb_diary_day");

            migrationBuilder.DropTable(
                name: "tb_weekly_check_in");
        }
    }
}
