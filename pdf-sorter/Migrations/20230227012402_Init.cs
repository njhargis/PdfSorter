using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pdf_sorter.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcessEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompleteTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedZips",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    ProcessEventId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LastUpdateDateTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedZips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessedZips_ProcessEvents_ProcessEventId",
                        column: x => x.ProcessEventId,
                        principalTable: "ProcessEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    ProcessedZipId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProcessedDateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PONumber = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessedFiles_ProcessedZips_ProcessedZipId",
                        column: x => x.ProcessedZipId,
                        principalTable: "ProcessedZips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedFiles_ProcessedZipId",
                table: "ProcessedFiles",
                column: "ProcessedZipId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedZips_ProcessEventId",
                table: "ProcessedZips",
                column: "ProcessEventId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessedFiles");

            migrationBuilder.DropTable(
                name: "ProcessedZips");

            migrationBuilder.DropTable(
                name: "ProcessEvents");
        }
    }
}
