using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace fuquizlearn_api.Migrations
{
    /// <inheritdoc />
    public partial class embededColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Vector>(
                name: "Embedding",
                table: "QuizBanks",
                type: "vector(768)",
                nullable: true);
            migrationBuilder.AddColumn<Vector>(
                name: "Embedding",
                table: "Quizes",
                type: "vector(768)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
