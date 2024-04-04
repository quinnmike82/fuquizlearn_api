using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace fuquizlearn_api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGame3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnswerHistories_Quizes_QuizId",
                table: "AnswerHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_GameQuizs_Quizes_QuizId",
                table: "GameQuizs");

            migrationBuilder.DropIndex(
                name: "IX_GameQuizs_QuizId",
                table: "GameQuizs");

            migrationBuilder.DropColumn(
                name: "QuizId",
                table: "GameQuizs");

            migrationBuilder.RenameColumn(
                name: "QuizId",
                table: "AnswerHistories",
                newName: "GameQuizId");

            migrationBuilder.RenameIndex(
                name: "IX_AnswerHistories_QuizId",
                table: "AnswerHistories",
                newName: "IX_AnswerHistories_GameQuizId");

            migrationBuilder.AddColumn<List<string>>(
                name: "Answers",
                table: "GameQuizs",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "CorrectAnswers",
                table: "GameQuizs",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "Questions",
                table: "GameQuizs",
                type: "text[]",
                nullable: false);

            migrationBuilder.DropColumn(
                name: "UserAnswer",
                table: "AnswerHistories");

            migrationBuilder.AddColumn<List<string>>(
                name: "UserAnswer",
                table: "AnswerHistories",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddForeignKey(
                name: "FK_AnswerHistories_GameQuizs_GameQuizId",
                table: "AnswerHistories",
                column: "GameQuizId",
                principalTable: "GameQuizs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnswerHistories_GameQuizs_GameQuizId",
                table: "AnswerHistories");

            migrationBuilder.DropColumn(
                name: "Answers",
                table: "GameQuizs");

            migrationBuilder.DropColumn(
                name: "CorrectAnswers",
                table: "GameQuizs");

            migrationBuilder.DropColumn(
                name: "Questions",
                table: "GameQuizs");

            migrationBuilder.RenameColumn(
                name: "GameQuizId",
                table: "AnswerHistories",
                newName: "QuizId");

            migrationBuilder.RenameIndex(
                name: "IX_AnswerHistories_GameQuizId",
                table: "AnswerHistories",
                newName: "IX_AnswerHistories_QuizId");

            migrationBuilder.AddColumn<int>(
                name: "QuizId",
                table: "GameQuizs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "UserAnswer",
                table: "AnswerHistories",
                type: "text",
                nullable: false,
                oldClrType: typeof(string[]),
                oldType: "text[]");

            migrationBuilder.CreateIndex(
                name: "IX_GameQuizs_QuizId",
                table: "GameQuizs",
                column: "QuizId");

            migrationBuilder.AddForeignKey(
                name: "FK_AnswerHistories_Quizes_QuizId",
                table: "AnswerHistories",
                column: "QuizId",
                principalTable: "Quizes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GameQuizs_Quizes_QuizId",
                table: "GameQuizs",
                column: "QuizId",
                principalTable: "Quizes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
