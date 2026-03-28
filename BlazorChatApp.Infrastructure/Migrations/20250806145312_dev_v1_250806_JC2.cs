using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorChatApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class dev_v1_250806_JC2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMembers_ChatRoom_GroupChatRoomId",
                table: "ChatMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatRoom_ChatMembers_ChatMember1Id",
                table: "ChatRoom");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatRoom_ChatMembers_ChatMember2Id",
                table: "ChatRoom");

            migrationBuilder.DropIndex(
                name: "IX_ChatRoom_ChatMember1Id",
                table: "ChatRoom");

            migrationBuilder.DropIndex(
                name: "IX_ChatRoom_ChatMember2Id",
                table: "ChatRoom");

            migrationBuilder.DropIndex(
                name: "IX_ChatMembers_GroupChatRoomId",
                table: "ChatMembers");

            migrationBuilder.DropColumn(
                name: "ChatMember1Id",
                table: "ChatRoom");

            migrationBuilder.DropColumn(
                name: "ChatMember2Id",
                table: "ChatRoom");

            migrationBuilder.DropColumn(
                name: "GroupChatRoomId",
                table: "ChatMembers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ChatMember1Id",
                table: "ChatRoom",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ChatMember2Id",
                table: "ChatRoom",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GroupChatRoomId",
                table: "ChatMembers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatRoom_ChatMember1Id",
                table: "ChatRoom",
                column: "ChatMember1Id");

            migrationBuilder.CreateIndex(
                name: "IX_ChatRoom_ChatMember2Id",
                table: "ChatRoom",
                column: "ChatMember2Id");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMembers_GroupChatRoomId",
                table: "ChatMembers",
                column: "GroupChatRoomId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMembers_ChatRoom_GroupChatRoomId",
                table: "ChatMembers",
                column: "GroupChatRoomId",
                principalTable: "ChatRoom",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatRoom_ChatMembers_ChatMember1Id",
                table: "ChatRoom",
                column: "ChatMember1Id",
                principalTable: "ChatMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatRoom_ChatMembers_ChatMember2Id",
                table: "ChatRoom",
                column: "ChatMember2Id",
                principalTable: "ChatMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
