using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KonXProWebApp.Migrations.db_9f8bee_konxdev
{
    /// <inheritdoc />
    public partial class AddHomeImprovementContractors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HomeImprovementContractors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LicenseNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BusinessName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DbaTradeName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    BusinessUniqueId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LicenseStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LicenseIssueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LicenseExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ContactPhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AddressBuilding = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AddressStreetName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AddressCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AddressState = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AddressZip = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Borough = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IngestedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeImprovementContractors", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HomeImprovementContractors");
        }
    }
}
