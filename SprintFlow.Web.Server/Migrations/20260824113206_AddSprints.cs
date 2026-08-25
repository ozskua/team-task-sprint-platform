using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SprintFlow.Web.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddSprints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SprintId",
                table: "Tasks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Sprints",
                columns: table => new
                {
                    SprintId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BoardId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sprints", x => x.SprintId);
                    table.UniqueConstraint("AK_Sprints_SprintId_BoardId", x => new { x.SprintId, x.BoardId });
                    table.CheckConstraint("CK_Sprints_EndAfterStart", "[EndDate] > [StartDate]");
                    table.ForeignKey(
                        name: "FK_Sprints_Boards_BoardId",
                        column: x => x.BoardId,
                        principalTable: "Boards",
                        principalColumn: "BoardId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sprints_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_SprintId_BoardId",
                table: "Tasks",
                columns: new[] { "SprintId", "BoardId" });

            migrationBuilder.CreateIndex(
                name: "IX_Sprints_BoardId_StartDate",
                table: "Sprints",
                columns: new[] { "BoardId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Sprints_CreatedByUserId",
                table: "Sprints",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Sprints_SprintId_BoardId",
                table: "Tasks",
                columns: new[] { "SprintId", "BoardId" },
                principalTable: "Sprints",
                principalColumns: new[] { "SprintId", "BoardId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Sprints_SprintId_BoardId",
                table: "Tasks");

            migrationBuilder.DropTable(
                name: "Sprints");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_SprintId_BoardId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "SprintId",
                table: "Tasks");
        }
    }
}
