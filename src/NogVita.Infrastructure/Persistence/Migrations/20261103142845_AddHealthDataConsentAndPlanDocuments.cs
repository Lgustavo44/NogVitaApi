using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NogVita.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHealthDataConsentAndPlanDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "health_data_consent_at_utc",
                table: "tb_patient_profile",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "health_data_consent_version",
                table: "tb_patient_profile",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tb_plan_document",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issued_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    nutritionist_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    crn_region = table.Column<int>(type: "integer", nullable: false),
                    crn_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_plan_document", x => x.id);
                    table.ForeignKey(
                        name: "fk_tb_plan_document_tb_nutrition_plan_plan_id",
                        column: x => x.plan_id,
                        principalTable: "tb_nutrition_plan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tb_plan_document_plan_id",
                table: "tb_plan_document",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "ux_plan_document_code",
                table: "tb_plan_document",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tb_plan_document");

            migrationBuilder.DropColumn(
                name: "health_data_consent_at_utc",
                table: "tb_patient_profile");

            migrationBuilder.DropColumn(
                name: "health_data_consent_version",
                table: "tb_patient_profile");
        }
    }
}
