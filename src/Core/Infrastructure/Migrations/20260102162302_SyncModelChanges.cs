using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mootable.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_RabbitHoles_RabbitHoleId",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_RabbitHoles_Messages_StarterMessageId",
                table: "RabbitHoles");

            migrationBuilder.DropForeignKey(
                name: "FK_RabbitHoles_MootTables_MootTableId",
                table: "RabbitHoles");

            migrationBuilder.DropIndex(
                name: "IX_RabbitHoles_StarterMessageId",
                table: "RabbitHoles");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "RabbitHoles");

            migrationBuilder.DropColumn(
                name: "StarterMessageId",
                table: "RabbitHoles");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "RabbitHoles");

            migrationBuilder.RenameColumn(
                name: "IsResolved",
                table: "RabbitHoles",
                newName: "IsPublic");

            migrationBuilder.AlterColumn<Guid>(
                name: "MootTableId",
                table: "RabbitHoles",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "ActiveExplorers",
                table: "RabbitHoles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ColorHex",
                table: "RabbitHoles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DepthLevel",
                table: "RabbitHoles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "RabbitHoles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "RabbitHoles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MinimumEnlightenmentScore",
                table: "RabbitHoles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "RabbitHoles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ParentId",
                table: "RabbitHoles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PinnedMessage",
                table: "RabbitHoles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PostCount",
                table: "RabbitHoles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "RabbitHoles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "RabbitHoles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Topic",
                table: "RabbitHoles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "RabbitHoleFollowers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RabbitHoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FollowedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NotifyOnNewPosts = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyOnTrending = table.Column<bool>(type: "boolean", nullable: false),
                    EngagementLevel = table.Column<int>(type: "integer", nullable: false),
                    LastVisitedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RabbitHoleFollowers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RabbitHoleFollowers_RabbitHoles_RabbitHoleId",
                        column: x => x.RabbitHoleId,
                        principalTable: "RabbitHoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RabbitHoleFollowers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RabbitHolePosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    RabbitHoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentPostId = table.Column<Guid>(type: "uuid", nullable: true),
                    DepthScore = table.Column<int>(type: "integer", nullable: false),
                    TruthScore = table.Column<int>(type: "integer", nullable: false),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    IsRedPill = table.Column<bool>(type: "boolean", nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: false),
                    MediaUrls = table.Column<string>(type: "text", nullable: true),
                    FollowerCount = table.Column<int>(type: "integer", nullable: false),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    IsEdited = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RabbitHolePosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RabbitHolePosts_RabbitHolePosts_ParentPostId",
                        column: x => x.ParentPostId,
                        principalTable: "RabbitHolePosts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RabbitHolePosts_RabbitHoles_RabbitHoleId",
                        column: x => x.RabbitHoleId,
                        principalTable: "RabbitHoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RabbitHolePosts_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RabbitHolePostReactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReactionType = table.Column<string>(type: "text", nullable: false),
                    ReactedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RabbitHolePostReactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RabbitHolePostReactions_RabbitHolePosts_PostId",
                        column: x => x.PostId,
                        principalTable: "RabbitHolePosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RabbitHolePostReactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RabbitHoles_ParentId",
                table: "RabbitHoles",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_RabbitHoleFollowers_RabbitHoleId",
                table: "RabbitHoleFollowers",
                column: "RabbitHoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RabbitHoleFollowers_UserId",
                table: "RabbitHoleFollowers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RabbitHolePostReactions_PostId",
                table: "RabbitHolePostReactions",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_RabbitHolePostReactions_UserId",
                table: "RabbitHolePostReactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RabbitHolePosts_AuthorId",
                table: "RabbitHolePosts",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_RabbitHolePosts_ParentPostId",
                table: "RabbitHolePosts",
                column: "ParentPostId");

            migrationBuilder.CreateIndex(
                name: "IX_RabbitHolePosts_RabbitHoleId",
                table: "RabbitHolePosts",
                column: "RabbitHoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_RabbitHoles_RabbitHoleId",
                table: "Messages",
                column: "RabbitHoleId",
                principalTable: "RabbitHoles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RabbitHoles_MootTables_MootTableId",
                table: "RabbitHoles",
                column: "MootTableId",
                principalTable: "MootTables",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RabbitHoles_RabbitHoles_ParentId",
                table: "RabbitHoles",
                column: "ParentId",
                principalTable: "RabbitHoles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_RabbitHoles_RabbitHoleId",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_RabbitHoles_MootTables_MootTableId",
                table: "RabbitHoles");

            migrationBuilder.DropForeignKey(
                name: "FK_RabbitHoles_RabbitHoles_ParentId",
                table: "RabbitHoles");

            migrationBuilder.DropTable(
                name: "RabbitHoleFollowers");

            migrationBuilder.DropTable(
                name: "RabbitHolePostReactions");

            migrationBuilder.DropTable(
                name: "RabbitHolePosts");

            migrationBuilder.DropIndex(
                name: "IX_RabbitHoles_ParentId",
                table: "RabbitHoles");

            migrationBuilder.DropColumn(
                name: "ActiveExplorers",
                table: "RabbitHoles");

            migrationBuilder.DropColumn(
                name: "ColorHex",
                table: "RabbitHoles");

            migrationBuilder.DropColumn(
                name: "DepthLevel",
                table: "RabbitHoles");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "RabbitHoles");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "RabbitHoles");

            migrationBuilder.DropColumn(
                name: "MinimumEnlightenmentScore",
                table: "RabbitHoles");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "RabbitHoles");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "RabbitHoles");

            migrationBuilder.DropColumn(
                name: "PinnedMessage",
                table: "RabbitHoles");

            migrationBuilder.DropColumn(
                name: "PostCount",
                table: "RabbitHoles");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "RabbitHoles");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "RabbitHoles");

            migrationBuilder.DropColumn(
                name: "Topic",
                table: "RabbitHoles");

            migrationBuilder.RenameColumn(
                name: "IsPublic",
                table: "RabbitHoles",
                newName: "IsResolved");

            migrationBuilder.AlterColumn<Guid>(
                name: "MootTableId",
                table: "RabbitHoles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "RabbitHoles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "StarterMessageId",
                table: "RabbitHoles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "RabbitHoles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_RabbitHoles_StarterMessageId",
                table: "RabbitHoles",
                column: "StarterMessageId",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_RabbitHoles_RabbitHoleId",
                table: "Messages",
                column: "RabbitHoleId",
                principalTable: "RabbitHoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RabbitHoles_Messages_StarterMessageId",
                table: "RabbitHoles",
                column: "StarterMessageId",
                principalTable: "Messages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RabbitHoles_MootTables_MootTableId",
                table: "RabbitHoles",
                column: "MootTableId",
                principalTable: "MootTables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
