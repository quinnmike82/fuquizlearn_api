using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fuquizlearn_api.Migrations
{
    /// <inheritdoc />
    public partial class updatePostView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int[]>(
                name: "ViewIds",
                table: "Posts",
                type: "integer[]",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ViewIds",
                table: "Posts");
        }
    }
}
