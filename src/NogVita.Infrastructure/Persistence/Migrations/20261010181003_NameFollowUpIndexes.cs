using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NogVita.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NameFollowUpIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "ix_tb_nutritionist_request_patient_id1",
                table: "tb_nutritionist_request",
                newName: "ux_nutritionist_request_patient_pending");

            migrationBuilder.RenameIndex(
                name: "ix_tb_nutritionist_request_patient_id",
                table: "tb_nutritionist_request",
                newName: "ix_nutritionist_request_patient_id");

            migrationBuilder.RenameIndex(
                name: "ix_tb_nutritionist_request_nutritionist_id_status",
                table: "tb_nutritionist_request",
                newName: "ix_nutritionist_request_nutritionist_status");

            migrationBuilder.RenameIndex(
                name: "ix_tb_care_relationship_patient_id",
                table: "tb_care_relationship",
                newName: "ux_care_relationship_patient_active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "ux_nutritionist_request_patient_pending",
                table: "tb_nutritionist_request",
                newName: "ix_tb_nutritionist_request_patient_id1");

            migrationBuilder.RenameIndex(
                name: "ix_nutritionist_request_patient_id",
                table: "tb_nutritionist_request",
                newName: "ix_tb_nutritionist_request_patient_id");

            migrationBuilder.RenameIndex(
                name: "ix_nutritionist_request_nutritionist_status",
                table: "tb_nutritionist_request",
                newName: "ix_tb_nutritionist_request_nutritionist_id_status");

            migrationBuilder.RenameIndex(
                name: "ux_care_relationship_patient_active",
                table: "tb_care_relationship",
                newName: "ix_tb_care_relationship_patient_id");
        }
    }
}
