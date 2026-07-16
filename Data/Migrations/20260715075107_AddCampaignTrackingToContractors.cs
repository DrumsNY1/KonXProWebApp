using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KonXProWebApp.Migrations.db_9f8bee_konxdev
{
    /// <inheritdoc />
    public partial class AddCampaignTrackingToContractors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CampaignCohort",
                table: "HomeImprovementContractors",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CampaignConverted",
                table: "HomeImprovementContractors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CampaignConvertedAt",
                table: "HomeImprovementContractors",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CampaignVisited",
                table: "HomeImprovementContractors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CampaignVisitedAt",
                table: "HomeImprovementContractors",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CampaignCohort",
                table: "HomeImprovementContractors");

            migrationBuilder.DropColumn(
                name: "CampaignConverted",
                table: "HomeImprovementContractors");

            migrationBuilder.DropColumn(
                name: "CampaignConvertedAt",
                table: "HomeImprovementContractors");

            migrationBuilder.DropColumn(
                name: "CampaignVisited",
                table: "HomeImprovementContractors");

            migrationBuilder.DropColumn(
                name: "CampaignVisitedAt",
                table: "HomeImprovementContractors");
        }
    }
}
