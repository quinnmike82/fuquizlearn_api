using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fuquizlearn_api.Migrations
{
    /// <inheritdoc />
    public partial class modifyQuizBank : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quiz_QuizBanks_QuizBankId",
                table: "Quiz");

            migrationBuilder.AlterColumn<int>(
                name: "QuizBankId",
                table: "Quiz",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Quiz_QuizBanks_QuizBankId",
                table: "Quiz",
                column: "QuizBankId",
                principalTable: "QuizBanks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quiz_QuizBanks_QuizBankId",
                table: "Quiz");

            migrationBuilder.AlterColumn<int>(
                name: "QuizBankId",
                table: "Quiz",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_Quiz_QuizBanks_QuizBankId",
                table: "Quiz",
                column: "QuizBankId",
                principalTable: "QuizBanks",
                principalColumn: "Id");
        }
    }
}
