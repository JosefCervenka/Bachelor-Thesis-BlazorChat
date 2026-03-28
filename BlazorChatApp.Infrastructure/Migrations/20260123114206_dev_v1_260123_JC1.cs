using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BlazorChatApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class dev_v1_260123_JC1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChatMemberRoleId",
                table: "ChatMembers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "ChatMembers",
                type: "character varying(21)",
                maxLength: 21,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ChatMemberRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMemberRoles", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ChatMemberRoles",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "User" },
                    { 2, "Owner" },
                    { 3, "Admin" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMembers_ChatMemberRoleId",
                table: "ChatMembers",
                column: "ChatMemberRoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMembers_ChatMemberRoles_ChatMemberRoleId",
                table: "ChatMembers",
                column: "ChatMemberRoleId",
                principalTable: "ChatMemberRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMembers_ChatMemberRoles_ChatMemberRoleId",
                table: "ChatMembers");

            migrationBuilder.DropTable(
                name: "ChatMemberRoles");

            migrationBuilder.DropIndex(
                name: "IX_ChatMembers_ChatMemberRoleId",
                table: "ChatMembers");

            migrationBuilder.DropColumn(
                name: "ChatMemberRoleId",
                table: "ChatMembers");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "ChatMembers");
        }
    }
}
