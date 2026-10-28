using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NogVita.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminBlockAndAuditTrail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "deactivated_at_utc",
                table: "tb_user",
                type: "timestamp with time zone",
                nullable: true);

            // Antes do D163, "desativado pelo admin" era o e-mail confirmado com a conta inativa.
            // Essas contas passam a ter o bloqueio explícito; a data exata não existia, então vale a da última alteração.
            migrationBuilder.Sql("""
                UPDATE tb_user
                SET deactivated_at_utc = COALESCE(updated_at, now())
                WHERE is_active = false AND email_confirmed_at_utc IS NOT NULL;
                """);

            migrationBuilder.CreateTable(
                name: "tb_admin_audit_entry",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    admin_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    target_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    details = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tb_admin_audit_entry", x => x.id);
                    table.ForeignKey(
                        name: "fk_tb_admin_audit_entry_users_admin_user_id",
                        column: x => x.admin_user_id,
                        principalTable: "tb_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tb_admin_audit_entry_admin_user_id",
                table: "tb_admin_audit_entry",
                column: "admin_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_tb_admin_audit_entry_target_user_id",
                table: "tb_admin_audit_entry",
                column: "target_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tb_admin_audit_entry");

            migrationBuilder.DropColumn(
                name: "deactivated_at_utc",
                table: "tb_user");
        }
    }
}
