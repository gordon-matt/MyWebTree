using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyWebTree.Data.MySql.Migrations
{
    /// <inheritdoc />
    public partial class RenamePersonNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "MyWebTree_People",
                newName: "GivenNames");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "MyWebTree_People",
                newName: "FamilyName");

            migrationBuilder.AddColumn<bool>(
                name: "IsFamilyNameFirst",
                table: "MyWebTree_People",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFamilyNameFirst",
                table: "MyWebTree_People");

            migrationBuilder.RenameColumn(
                name: "GivenNames",
                table: "MyWebTree_People",
                newName: "FirstName");

            migrationBuilder.RenameColumn(
                name: "FamilyName",
                table: "MyWebTree_People",
                newName: "LastName");
        }
    }
}
