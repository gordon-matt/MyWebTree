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
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MyWebTree_ParentSetMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    TreeId = table.Column<int>(type: "int", nullable: false),
                    ParentSetId = table.Column<int>(type: "int", nullable: false),
                    ParentPersonId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MyWebTree_ParentSetMembers", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MyWebTree_ParentSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    TreeId = table.Column<int>(type: "int", nullable: false),
                    ChildPersonId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Date = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MyWebTree_ParentSets", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MyWebTree_People",
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
                    table.PrimaryKey("PK_MyWebTree_People", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MyWebTree_Relationships",
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
                    table.PrimaryKey("PK_MyWebTree_Relationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MyWebTree_Relationships_MyWebTree_People_FromPersonId",
                        column: x => x.FromPersonId,
                        principalTable: "MyWebTree_People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MyWebTree_Relationships_MyWebTree_People_ToPersonId",
                        column: x => x.ToPersonId,
                        principalTable: "MyWebTree_People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MyWebTree_Trees",
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
                    table.PrimaryKey("PK_MyWebTree_Trees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MyWebTree_Trees_MyWebTree_People_FocusPersonId",
                        column: x => x.FocusPersonId,
                        principalTable: "MyWebTree_People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_MyWebTree_ParentSetMembers_ParentPersonId",
                table: "MyWebTree_ParentSetMembers",
                column: "ParentPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_MyWebTree_ParentSetMembers_ParentSetId_ParentPersonId",
                table: "MyWebTree_ParentSetMembers",
                columns: new[] { "ParentSetId", "ParentPersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MyWebTree_ParentSetMembers_TreeId",
                table: "MyWebTree_ParentSetMembers",
                column: "TreeId");

            migrationBuilder.CreateIndex(
                name: "IX_MyWebTree_ParentSets_ChildPersonId",
                table: "MyWebTree_ParentSets",
                column: "ChildPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_MyWebTree_ParentSets_TreeId",
                table: "MyWebTree_ParentSets",
                column: "TreeId");

            migrationBuilder.CreateIndex(
                name: "IX_MyWebTree_People_TreeId",
                table: "MyWebTree_People",
                column: "TreeId");

            migrationBuilder.CreateIndex(
                name: "IX_MyWebTree_Relationships_FromPersonId_ToPersonId_Kind",
                table: "MyWebTree_Relationships",
                columns: new[] { "FromPersonId", "ToPersonId", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MyWebTree_Relationships_ToPersonId",
                table: "MyWebTree_Relationships",
                column: "ToPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_MyWebTree_Relationships_TreeId_Kind",
                table: "MyWebTree_Relationships",
                columns: new[] { "TreeId", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_MyWebTree_Trees_FocusPersonId",
                table: "MyWebTree_Trees",
                column: "FocusPersonId");

            migrationBuilder.AddForeignKey(
                name: "FK_MyWebTree_ParentSetMembers_MyWebTree_ParentSets_ParentSetId",
                table: "MyWebTree_ParentSetMembers",
                column: "ParentSetId",
                principalTable: "MyWebTree_ParentSets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MyWebTree_ParentSetMembers_MyWebTree_People_ParentPersonId",
                table: "MyWebTree_ParentSetMembers",
                column: "ParentPersonId",
                principalTable: "MyWebTree_People",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MyWebTree_ParentSets_MyWebTree_People_ChildPersonId",
                table: "MyWebTree_ParentSets",
                column: "ChildPersonId",
                principalTable: "MyWebTree_People",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MyWebTree_People_MyWebTree_Trees_TreeId",
                table: "MyWebTree_People",
                column: "TreeId",
                principalTable: "MyWebTree_Trees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MyWebTree_Trees_MyWebTree_People_FocusPersonId",
                table: "MyWebTree_Trees");

            migrationBuilder.DropTable(
                name: "MyWebTree_ParentSetMembers");

            migrationBuilder.DropTable(
                name: "MyWebTree_Relationships");

            migrationBuilder.DropTable(
                name: "MyWebTree_ParentSets");

            migrationBuilder.DropTable(
                name: "MyWebTree_People");

            migrationBuilder.DropTable(
                name: "MyWebTree_Trees");
        }
    }
}
