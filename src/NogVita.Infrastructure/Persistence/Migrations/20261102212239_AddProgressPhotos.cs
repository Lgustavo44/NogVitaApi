using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NogVita.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProgressPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "progress_photos_consent_at_utc",
                table: "tb_patient_profile",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "progress_photos_consent_version",
                table: "tb_patient_profile",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tb_progress_photo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    taken_on = table.Column<DateOnly>(type: "date", nullable: false),
                    angle = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    storage_path = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_progress_photo", x => x.id);
                    table.ForeignKey(
                        name: "fk_tb_progress_photo_users_patient_id",
                        column: x => x.patient_id,
                        principalTable: "tb_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_progress_photo_patient_day_angle",
                table: "tb_progress_photo",
                columns: new[] { "patient_id", "taken_on", "angle" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tb_progress_photo");

            migrationBuilder.DropColumn(
                name: "progress_photos_consent_at_utc",
                table: "tb_patient_profile");

            migrationBuilder.DropColumn(
                name: "progress_photos_consent_version",
                table: "tb_patient_profile");
        }
    }
}
