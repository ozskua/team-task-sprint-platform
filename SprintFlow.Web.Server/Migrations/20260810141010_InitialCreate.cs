using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SprintFlow.Web.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RoleId", "RoleName" },
                values: new object[,]
                {
                    { 1, "Admin" },
                    { 2, "Member" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "CreatedDate", "Email", "FirstName", "IsActive", "LastName", "PasswordHash", "RoleId" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ayse.demir@sprintflow.local", "Ayşe", true, "Demir", "$2a$12$PAqHNBJ/AqN4D9UJsktCKerPo2bqhqnRxLj6VOmLHxLPbMiIIxn/a", 1 },
                    { 2, new DateTime(2026, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "mert.kaya@sprintflow.local", "Mert", true, "Kaya", "$2a$12$IEsozBUG66RHg4u5uwF6XuFZp4ZbcUQtvBP16wPZzf4X4h6TOljR2", 2 },
                    { 3, new DateTime(2026, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "elif.yildiz@sprintflow.local", "Elif", true, "Yıldız", "$2a$12$LZwgxyQpuU2NOWSjm.szSOXsiJgwT26b4LYRzI3xR/mAeGrU5pgQO", 2 },
                    { 4, new DateTime(2026, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "burak.sahin@sprintflow.local", "Burak", true, "Şahin", "$2a$12$yIOuFoEm4ZkFY1sbn6U4c.BoqbsqCLxg08mTwfiqCYz3sReIwL2tC", 2 },
                    { 5, new DateTime(2026, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "zeynep.arslan@sprintflow.local", "Zeynep", true, "Arslan", "$2a$12$d0DKlM7jjdirdf1Mk5aGWO5vETEwZqP29h3/S8s4TJtr29Z6EFC3G", 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Roles_RoleName",
                table: "Roles",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                table: "Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
