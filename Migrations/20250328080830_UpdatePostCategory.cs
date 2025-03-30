using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePostCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PostCategory_Categories_CategoryID",
                table: "PostCategory");

            migrationBuilder.DropForeignKey(
                name: "FK_PostCategory_Posts_PostID",
                table: "PostCategory");

            migrationBuilder.DropColumn(
                name: "IsMainCategory",
                table: "PostCategory");

            migrationBuilder.RenameColumn(
                name: "CategoryID",
                table: "PostCategory",
                newName: "CategoryId");

            migrationBuilder.RenameColumn(
                name: "PostID",
                table: "PostCategory",
                newName: "PostId");

            migrationBuilder.RenameIndex(
                name: "IX_PostCategory_CategoryID",
                table: "PostCategory",
                newName: "IX_PostCategory_CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_PostCategory_Categories_CategoryId",
                table: "PostCategory",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PostCategory_Posts_PostId",
                table: "PostCategory",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "PostId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PostCategory_Categories_CategoryId",
                table: "PostCategory");

            migrationBuilder.DropForeignKey(
                name: "FK_PostCategory_Posts_PostId",
                table: "PostCategory");

            migrationBuilder.RenameColumn(
                name: "CategoryId",
                table: "PostCategory",
                newName: "CategoryID");

            migrationBuilder.RenameColumn(
                name: "PostId",
                table: "PostCategory",
                newName: "PostID");

            migrationBuilder.RenameIndex(
                name: "IX_PostCategory_CategoryId",
                table: "PostCategory",
                newName: "IX_PostCategory_CategoryID");

            migrationBuilder.AddColumn<bool>(
                name: "IsMainCategory",
                table: "PostCategory",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_PostCategory_Categories_CategoryID",
                table: "PostCategory",
                column: "CategoryID",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PostCategory_Posts_PostID",
                table: "PostCategory",
                column: "PostID",
                principalTable: "Posts",
                principalColumn: "PostId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
