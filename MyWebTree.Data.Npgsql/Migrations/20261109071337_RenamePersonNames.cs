using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyWebTree.Data.Npgsql.Migrations
{
    /// <inheritdoc />
    public partial class RenamePersonNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FirstName",
                schema: "webtree",
                table: "People",
                newName: "GivenNames");

            migrationBuilder.RenameColumn(
                name: "LastName",
                schema: "webtree",
                table: "People",
                newName: "FamilyName");

            migrationBuilder.AddColumn<bool>(
                name: "IsFamilyNameFirst",
                schema: "webtree",
                table: "People",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFamilyNameFirst",
                schema: "webtree",
                table: "People");

            migrationBuilder.RenameColumn(
                name: "GivenNames",
                schema: "webtree",
                table: "People",
                newName: "FirstName");

            migrationBuilder.RenameColumn(
                name: "FamilyName",
                schema: "webtree",
                table: "People",
                newName: "LastName");
        }
    }
}
