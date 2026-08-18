using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SprintFlow.Web.Server.Migrations
{
    /// <inheritdoc />
    public partial class EnforceTeamOwnerMatchesBoardCreator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Boards_BoardId",
                table: "Teams");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Boards_BoardId_CreatedByUserId",
                table: "Boards",
                columns: new[] { "BoardId", "CreatedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Teams_BoardId_CreatedByUserId",
                table: "Teams",
                columns: new[] { "BoardId", "CreatedByUserId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Boards_BoardId_CreatedByUserId",
                table: "Teams",
                columns: new[] { "BoardId", "CreatedByUserId" },
                principalTable: "Boards",
                principalColumns: new[] { "BoardId", "CreatedByUserId" },
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Boards_BoardId_CreatedByUserId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Teams_BoardId_CreatedByUserId",
                table: "Teams");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Boards_BoardId_CreatedByUserId",
                table: "Boards");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Boards_BoardId",
                table: "Teams",
                column: "BoardId",
                principalTable: "Boards",
                principalColumn: "BoardId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
