using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fuquizlearn_api.Migrations
{
    /// <inheritdoc />
    public partial class modifyQuizModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Choices",
                table: "Quizes");

            migrationBuilder.RenameColumn(
                name: "descrition",
                table: "QuizBanks",
                newName: "Description");

            migrationBuilder.AddColumn<string>(
                name: "Answer",
                table: "Quizes",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Answer",
                table: "Quizes");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "QuizBanks",
                newName: "descrition");

            migrationBuilder.AddColumn<string>(
                name: "Choices",
                table: "Quizes",
                type: "jsonb",
                nullable: false,
                defaultValue: "");
        }
    }
}
