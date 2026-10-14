using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NogVita.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientRoutineAndMealTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "bed_time",
                table: "tb_patient_profile",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "training_time",
                table: "tb_patient_profile",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "wake_up_time",
                table: "tb_patient_profile",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "suggested_time",
                table: "tb_meal",
                type: "time without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bed_time",
                table: "tb_patient_profile");

            migrationBuilder.DropColumn(
                name: "training_time",
                table: "tb_patient_profile");

            migrationBuilder.DropColumn(
                name: "wake_up_time",
                table: "tb_patient_profile");

            migrationBuilder.DropColumn(
                name: "suggested_time",
                table: "tb_meal");
        }
    }
}
