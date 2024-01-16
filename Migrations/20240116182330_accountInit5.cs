using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fuquizlearn_api.Migrations
{
    /// <inheritdoc />
    public partial class accountInit5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptTerms",
                table: "Accounts");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Accounts",
                newName: "Username");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "Accounts",
                newName: "FullName");

            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "Accounts",
                newName: "Avatar");

            migrationBuilder.AddColumn<DateTime>(
                name: "Dob",
                table: "Accounts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<List<int>>(
                name: "FavoriteBankIds",
                table: "Accounts",
                type: "integer[]",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "useAICount",
                table: "Accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Dob",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "FavoriteBankIds",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "useAICount",
                table: "Accounts");

            migrationBuilder.RenameColumn(
                name: "Username",
                table: "Accounts",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "Accounts",
                newName: "LastName");

            migrationBuilder.RenameColumn(
                name: "Avatar",
                table: "Accounts",
                newName: "FirstName");

            migrationBuilder.AddColumn<bool>(
                name: "AcceptTerms",
                table: "Accounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
