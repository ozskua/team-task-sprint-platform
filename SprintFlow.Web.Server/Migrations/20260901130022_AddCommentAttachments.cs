using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SprintFlow.Web.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddCommentAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TaskCommentId",
                table: "TaskAttachments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskAttachments_TaskCommentId",
                table: "TaskAttachments",
                column: "TaskCommentId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskAttachments_TaskComments_TaskCommentId",
                table: "TaskAttachments",
                column: "TaskCommentId",
                principalTable: "TaskComments",
                principalColumn: "TaskCommentId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskAttachments_TaskComments_TaskCommentId",
                table: "TaskAttachments");

            migrationBuilder.DropIndex(
                name: "IX_TaskAttachments_TaskCommentId",
                table: "TaskAttachments");

            migrationBuilder.DropColumn(
                name: "TaskCommentId",
                table: "TaskAttachments");
        }
    }
}
