using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MyWebTree.Data.Npgsql.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "webtree");

            migrationBuilder.CreateTable(
                name: "People",
                schema: "webtree",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TreeId = table.Column<int>(type: "integer", nullable: false),
                    Gender = table.Column<int>(type: "integer", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LastName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BirthDate = table.Column<string>(type: "character varying(10)", unicode: false, maxLength: 10, nullable: true),
                    DeathDate = table.Column<string>(type: "character varying(10)", unicode: false, maxLength: 10, nullable: true),
                    BirthPlace = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DeathPlace = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AvatarUrl = table.Column<string>(type: "character varying(500)", unicode: false, maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_People", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Relationships",
                schema: "webtree",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TreeId = table.Column<int>(type: "integer", nullable: false),
                    FromPersonId = table.Column<int>(type: "integer", nullable: false),
                    ToPersonId = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "Trees",
                schema: "webtree",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FocusPersonId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                });

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
