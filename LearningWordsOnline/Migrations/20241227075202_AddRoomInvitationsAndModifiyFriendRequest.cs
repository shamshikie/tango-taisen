using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningWordsOnline.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomInvitationsAndModifiyFriendRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastUpdatedAt",
                table: "FriendRequests");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReferencedAt",
                table: "FriendRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RespondedAt",
                table: "FriendRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RoomInvitations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoomId = table.Column<int>(type: "int", nullable: false),
                    AppUserId1 = table.Column<int>(type: "int", nullable: false),
                    AppUserId2 = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DismissedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReferencedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomInvitations_AppUsers_AppUserId1",
                        column: x => x.AppUserId1,
                        principalTable: "AppUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RoomInvitations_AppUsers_AppUserId2",
                        column: x => x.AppUserId2,
                        principalTable: "AppUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoomInvitations_AppUserId1",
                table: "RoomInvitations",
                column: "AppUserId1");

            migrationBuilder.CreateIndex(
                name: "IX_RoomInvitations_AppUserId2",
                table: "RoomInvitations",
                column: "AppUserId2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoomInvitations");

            migrationBuilder.DropColumn(
                name: "ReferencedAt",
                table: "FriendRequests");

            migrationBuilder.DropColumn(
                name: "RespondedAt",
                table: "FriendRequests");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedAt",
                table: "FriendRequests",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
