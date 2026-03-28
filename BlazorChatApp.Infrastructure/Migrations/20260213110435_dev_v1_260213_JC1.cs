using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorChatApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class dev_v1_260213_JC1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "ProfilePicture",
                table: "ChatRoom",
                type: "bytea",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilePicture",
                table: "ChatRoom");
        }
    }
}
