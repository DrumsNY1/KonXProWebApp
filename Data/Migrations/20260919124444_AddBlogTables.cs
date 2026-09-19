using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KonXProWebApp.Migrations.db_9f8bee_konxdev
{
    /// <inheritdoc />
    public partial class AddBlogTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlogContent",
                schema: "dbo",
                columns: table => new
                {
                    ContentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompletionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SourceID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogContent", x => x.ContentID);
                });

            migrationBuilder.CreateTable(
                name: "BlogFeedSources",
                schema: "dbo",
                columns: table => new
                {
                    FeedID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeedName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FeedUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FeedCategory = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogFeedSources", x => x.FeedID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlogContent",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "BlogFeedSources",
                schema: "dbo");
        }
    }
}
