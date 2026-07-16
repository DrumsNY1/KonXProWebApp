using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KonXProWebApp.Migrations.db_9f8bee_konxdev
{
    /// <inheritdoc />
    public partial class AddMarketingFieldsToContractors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailAddress",
                table: "HomeImprovementContractors",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailSent",
                table: "HomeImprovementContractors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PostcardSent",
                table: "HomeImprovementContractors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SalesNotes",
                table: "HomeImprovementContractors",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SalesStatus",
                table: "HomeImprovementContractors",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailAddress",
                table: "HomeImprovementContractors");

            migrationBuilder.DropColumn(
                name: "EmailSent",
                table: "HomeImprovementContractors");

            migrationBuilder.DropColumn(
                name: "PostcardSent",
                table: "HomeImprovementContractors");

            migrationBuilder.DropColumn(
                name: "SalesNotes",
                table: "HomeImprovementContractors");

            migrationBuilder.DropColumn(
                name: "SalesStatus",
                table: "HomeImprovementContractors");
        }
    }
}
