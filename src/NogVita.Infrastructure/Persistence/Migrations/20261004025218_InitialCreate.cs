using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NogVita.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tb_user",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    cpf = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_admin = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_user", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tb_nutritionist_profile",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    crn_region = table.Column<int>(type: "integer", nullable: false),
                    crn_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_nutritionist_profile", x => x.id);
                    table.ForeignKey(
                        name: "fk_tb_nutritionist_profile_users_user_id",
                        column: x => x.user_id,
                        principalTable: "tb_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_patient_profile",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: false),
                    biological_sex = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    height_in_cm = table.Column<int>(type: "integer", nullable: false),
                    goal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_patient_profile", x => x.id);
                    table.ForeignKey(
                        name: "fk_tb_patient_profile_users_user_id",
                        column: x => x.user_id,
                        principalTable: "tb_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tb_nutritionist_profile_crn_region_crn_number",
                table: "tb_nutritionist_profile",
                columns: new[] { "crn_region", "crn_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tb_nutritionist_profile_user_id",
                table: "tb_nutritionist_profile",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tb_patient_profile_user_id",
                table: "tb_patient_profile",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tb_user_cpf",
                table: "tb_user",
                column: "cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tb_user_email",
                table: "tb_user",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tb_nutritionist_profile");

            migrationBuilder.DropTable(
                name: "tb_patient_profile");

            migrationBuilder.DropTable(
                name: "tb_user");
        }
    }
}
