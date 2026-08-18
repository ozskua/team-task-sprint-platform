using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SprintFlow.Web.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddTasksAndWorkflowColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkflowColumns",
                columns: table => new
                {
                    WorkflowColumnId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BoardId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    SystemKind = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowColumns", x => x.WorkflowColumnId);
                    table.UniqueConstraint("AK_WorkflowColumns_WorkflowColumnId_BoardId", x => new { x.WorkflowColumnId, x.BoardId });
                    table.ForeignKey(
                        name: "FK_WorkflowColumns_Boards_BoardId",
                        column: x => x.BoardId,
                        principalTable: "Boards",
                        principalColumn: "BoardId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    TaskId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    BoardId = table.Column<int>(type: "int", nullable: false),
                    TaskColumnId = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.TaskId);
                    table.ForeignKey(
                        name: "FK_Tasks_Boards_BoardId",
                        column: x => x.BoardId,
                        principalTable: "Boards",
                        principalColumn: "BoardId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Tasks_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tasks_WorkflowColumns_TaskColumnId_BoardId",
                        columns: x => new { x.TaskColumnId, x.BoardId },
                        principalTable: "WorkflowColumns",
                        principalColumns: new[] { "WorkflowColumnId", "BoardId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaskParticipants",
                columns: table => new
                {
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AddedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskParticipants", x => new { x.TaskId, x.UserId });
                    table.ForeignKey(
                        name: "FK_TaskParticipants_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "TaskId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskParticipants_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskParticipants_UserId",
                table: "TaskParticipants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_BoardId",
                table: "Tasks",
                column: "BoardId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_CreatedByUserId",
                table: "Tasks",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_TaskColumnId_BoardId",
                table: "Tasks",
                columns: new[] { "TaskColumnId", "BoardId" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_TaskColumnId_Position",
                table: "Tasks",
                columns: new[] { "TaskColumnId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowColumns_BoardId_Position",
                table: "WorkflowColumns",
                columns: new[] { "BoardId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowColumns_BoardId_SystemKind",
                table: "WorkflowColumns",
                columns: new[] { "BoardId", "SystemKind" },
                unique: true,
                filter: "[SystemKind] IS NOT NULL");

            // Boards created before this migration have no columns, and every board is required to
            // have all three. New boards get them from BoardService; existing ones get them here,
            // so the rule holds for the whole table the moment the migration finishes.
            //
            // Only inserts into the table created above — no existing Area, Board, Team, User or
            // Role row is read for anything but its id, and none is modified.
            //
            // NOT EXISTS makes the statement idempotent. The table is empty at this point so the
            // guard changes nothing today; it is here so re-running the statement by hand during
            // verification cannot produce a duplicate and trip the filtered unique index.
            //
            // CreatedDate is left out on purpose: the column default supplies it.
            migrationBuilder.Sql("""
                INSERT INTO WorkflowColumns (BoardId, Name, Position, SystemKind)
                SELECT b.BoardId, seed.Name, seed.Position, seed.SystemKind
                FROM   Boards AS b
                CROSS JOIN (VALUES
                        (N'To Do',     0, 1),
                        (N'Completed', 1, 2),
                        (N'Cancelled', 2, 3)
                    ) AS seed (Name, Position, SystemKind)
                WHERE NOT EXISTS (
                    SELECT 1 FROM WorkflowColumns AS existing
                    WHERE existing.BoardId = b.BoardId
                      AND existing.SystemKind = seed.SystemKind);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskParticipants");

            migrationBuilder.DropTable(
                name: "Tasks");

            migrationBuilder.DropTable(
                name: "WorkflowColumns");
        }
    }
}
