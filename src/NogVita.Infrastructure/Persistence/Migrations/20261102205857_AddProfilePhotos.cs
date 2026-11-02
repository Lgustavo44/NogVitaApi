using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NogVita.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProfilePhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "photo_path",
                table: "tb_patient_profile",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "photo_path",
                table: "tb_nutritionist_profile",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "photo_path",
                table: "tb_patient_profile");

            migrationBuilder.DropColumn(
                name: "photo_path",
                table: "tb_nutritionist_profile");
        }
    }
}
