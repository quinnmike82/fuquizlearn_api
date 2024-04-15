using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fuquizlearn_api.Migrations
{
    /// <inheritdoc />
    public partial class updateDeleteReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Reports",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Reports");
        }
    }
}
