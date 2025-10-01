using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningWordsOnline.Migrations
{
    /// <inheritdoc />
    public partial class ModifiedBattleAppUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_BattleAppUsers",
                table: "BattleAppUsers");

            migrationBuilder.DropIndex(
                name: "IX_BattleAppUsers_BattleId",
                table: "BattleAppUsers");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "BattleAppUsers");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BattleAppUsers",
                table: "BattleAppUsers",
                columns: ["BattleId", "AppUserId"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_BattleAppUsers",
                table: "BattleAppUsers");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "BattleAppUsers",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BattleAppUsers",
                table: "BattleAppUsers",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_BattleAppUsers_BattleId",
                table: "BattleAppUsers",
                column: "BattleId");
        }
    }
}
