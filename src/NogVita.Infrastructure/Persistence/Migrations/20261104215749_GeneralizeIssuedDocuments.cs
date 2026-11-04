using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NogVita.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GeneralizeIssuedDocuments : Migration
    {
        // Renomeia, em vez de recriar: os códigos dos PDFs de plano já emitidos continuam valendo.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tb_plan_document_tb_nutrition_plan_plan_id",
                table: "tb_plan_document");

            migrationBuilder.DropIndex(
                name: "ix_tb_plan_document_plan_id",
                table: "tb_plan_document");

            migrationBuilder.RenameTable(
                name: "tb_plan_document",
                newName: "tb_issued_document");

            migrationBuilder.Sql("ALTER TABLE tb_issued_document RENAME CONSTRAINT pk_tb_plan_document TO pk_tb_issued_document;");

            migrationBuilder.RenameColumn(
                name: "plan_id",
                table: "tb_issued_document",
                newName: "source_id");

            migrationBuilder.RenameIndex(
                name: "ux_plan_document_code",
                table: "tb_issued_document",
                newName: "ux_issued_document_code");

            migrationBuilder.AddColumn<string>(
                name: "kind",
                table: "tb_issued_document",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "NutritionPlan");

            migrationBuilder.CreateIndex(
                name: "ix_tb_issued_document_kind_source_id",
                table: "tb_issued_document",
                columns: new[] { "kind", "source_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Os registros de anamnese não cabem na tabela antiga, que só aponta para planos.
            migrationBuilder.Sql("DELETE FROM tb_issued_document WHERE kind <> 'NutritionPlan';");

            migrationBuilder.DropIndex(
                name: "ix_tb_issued_document_kind_source_id",
                table: "tb_issued_document");

            migrationBuilder.DropColumn(
                name: "kind",
                table: "tb_issued_document");

            migrationBuilder.RenameIndex(
                name: "ux_issued_document_code",
                table: "tb_issued_document",
                newName: "ux_plan_document_code");

            migrationBuilder.RenameColumn(
                name: "source_id",
                table: "tb_issued_document",
                newName: "plan_id");

            migrationBuilder.Sql("ALTER TABLE tb_issued_document RENAME CONSTRAINT pk_tb_issued_document TO pk_tb_plan_document;");

            migrationBuilder.RenameTable(
                name: "tb_issued_document",
                newName: "tb_plan_document");

            migrationBuilder.CreateIndex(
                name: "ix_tb_plan_document_plan_id",
                table: "tb_plan_document",
                column: "plan_id");

            migrationBuilder.AddForeignKey(
                name: "fk_tb_plan_document_tb_nutrition_plan_plan_id",
                table: "tb_plan_document",
                column: "plan_id",
                principalTable: "tb_nutrition_plan",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
