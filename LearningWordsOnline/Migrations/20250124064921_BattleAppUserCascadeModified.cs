using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningWordsOnline.Migrations
{
    /// <inheritdoc />
    public partial class BattleAppUserCascadeModified : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BattleAppUsers_AppUsers_AppUserId",
                table: "BattleAppUsers");

            migrationBuilder.AddForeignKey(
                name: "FK_BattleAppUsers_AppUsers_AppUserId",
                table: "BattleAppUsers",
                column: "AppUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BattleAppUsers_AppUsers_AppUserId",
                table: "BattleAppUsers");

            migrationBuilder.AddForeignKey(
                name: "FK_BattleAppUsers_AppUsers_AppUserId",
                table: "BattleAppUsers",
                column: "AppUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
