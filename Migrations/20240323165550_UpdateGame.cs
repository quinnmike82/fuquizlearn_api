using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace fuquizlearn_api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGame : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_Classrooms_ClassroomId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "GameQuizs",
                table: "Games");

            migrationBuilder.AlterColumn<int>(
                name: "ClassroomId",
                table: "Games",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "Duration",
                table: "Games",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuizBankId",
                table: "Games",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "GameQuizs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GameId = table.Column<int>(type: "integer", nullable: false),
                    QuizId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameQuizs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameQuizs_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameQuizs_Quizes_QuizId",
                        column: x => x.QuizId,
                        principalTable: "Quizes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Games_QuizBankId",
                table: "Games",
                column: "QuizBankId");

            migrationBuilder.CreateIndex(
                name: "IX_GameQuizs_GameId",
                table: "GameQuizs",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_GameQuizs_QuizId",
                table: "GameQuizs",
                column: "QuizId");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_Classrooms_ClassroomId",
                table: "Games",
                column: "ClassroomId",
                principalTable: "Classrooms",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_QuizBanks_QuizBankId",
                table: "Games",
                column: "QuizBankId",
                principalTable: "QuizBanks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_Classrooms_ClassroomId",
                table: "Games");

            migrationBuilder.DropForeignKey(
                name: "FK_Games_QuizBanks_QuizBankId",
                table: "Games");

            migrationBuilder.DropTable(
                name: "GameQuizs");

            migrationBuilder.DropIndex(
                name: "IX_Games_QuizBankId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "QuizBankId",
                table: "Games");

            migrationBuilder.AlterColumn<int>(
                name: "ClassroomId",
                table: "Games",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GameQuizs",
                table: "Games",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Games_Classrooms_ClassroomId",
                table: "Games",
                column: "ClassroomId",
                principalTable: "Classrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
