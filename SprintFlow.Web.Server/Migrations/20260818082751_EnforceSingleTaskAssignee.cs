using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SprintFlow.Web.Server.Migrations
{
    /// <inheritdoc />
    public partial class EnforceSingleTaskAssignee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TaskParticipants_TaskId",
                table: "TaskParticipants",
                column: "TaskId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaskParticipants_TaskId",
                table: "TaskParticipants");
        }
    }
}
