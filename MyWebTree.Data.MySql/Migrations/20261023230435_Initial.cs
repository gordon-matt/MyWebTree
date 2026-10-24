using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace MyWebTree.Data.MySql.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "webtree");

            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "People",
                schema: "webtree",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    TreeId = table.Column<int>(type: "int", nullable: false),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    FirstName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    LastName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    BirthDate = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    DeathDate = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    BirthPlace = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    DeathPlace = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    AvatarUrl = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_People", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Relationships",
                schema: "webtree",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    TreeId = table.Column<int>(type: "int", nullable: false),
                    FromPersonId = table.Column<int>(type: "int", nullable: false),
                    ToPersonId = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Relationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Relationships_People_FromPersonId",
                        column: x => x.FromPersonId,
                        principalSchema: "webtree",
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Relationships_People_ToPersonId",
                        column: x => x.ToPersonId,
                        principalSchema: "webtree",
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Trees",
                schema: "webtree",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    FocusPersonId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trees_People_FocusPersonId",
                        column: x => x.FocusPersonId,
                        principalSchema: "webtree",
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_People_TreeId",
                schema: "webtree",
                table: "People",
                column: "TreeId");

            migrationBuilder.CreateIndex(
                name: "IX_Relationships_FromPersonId_ToPersonId_Kind",
                schema: "webtree",
                table: "Relationships",
                columns: new[] { "FromPersonId", "ToPersonId", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Relationships_ToPersonId",
                schema: "webtree",
                table: "Relationships",
                column: "ToPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Relationships_TreeId_Kind",
                schema: "webtree",
                table: "Relationships",
                columns: new[] { "TreeId", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_Trees_FocusPersonId",
                schema: "webtree",
                table: "Trees",
                column: "FocusPersonId");

            migrationBuilder.AddForeignKey(
                name: "FK_People_Trees_TreeId",
                schema: "webtree",
                table: "People",
                column: "TreeId",
                principalSchema: "webtree",
                principalTable: "Trees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_People_Trees_TreeId",
                schema: "webtree",
                table: "People");

            migrationBuilder.DropTable(
                name: "Relationships",
                schema: "webtree");

            migrationBuilder.DropTable(
                name: "Trees",
                schema: "webtree");

            migrationBuilder.DropTable(
                name: "People",
                schema: "webtree");
        }
    }
}
