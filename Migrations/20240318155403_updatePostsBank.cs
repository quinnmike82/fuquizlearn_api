using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fuquizlearn_api.Migrations
{
    /// <inheritdoc />
    public partial class updatePostsBank : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QuizBankId",
                table: "Posts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_QuizBankId",
                table: "Posts",
                column: "QuizBankId");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_QuizBanks_QuizBankId",
                table: "Posts",
                column: "QuizBankId",
                principalTable: "QuizBanks",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_QuizBanks_QuizBankId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_QuizBankId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "QuizBankId",
                table: "Posts");
        }
    }
}
