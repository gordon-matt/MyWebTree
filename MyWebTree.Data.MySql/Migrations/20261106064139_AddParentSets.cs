using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace MyWebTree.Data.MySql.Migrations
{
    /// <inheritdoc />
    public partial class AddParentSets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParentSets",
                schema: "webtree",
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
                    table.PrimaryKey("PK_ParentSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParentSets_People_ChildPersonId",
                        column: x => x.ChildPersonId,
                        principalSchema: "webtree",
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ParentSetMembers",
                schema: "webtree",
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
                    table.PrimaryKey("PK_ParentSetMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParentSetMembers_ParentSets_ParentSetId",
                        column: x => x.ParentSetId,
                        principalSchema: "webtree",
                        principalTable: "ParentSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParentSetMembers_People_ParentPersonId",
                        column: x => x.ParentPersonId,
                        principalSchema: "webtree",
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ParentSetMembers_ParentPersonId",
                schema: "webtree",
                table: "ParentSetMembers",
                column: "ParentPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentSetMembers_ParentSetId_ParentPersonId",
                schema: "webtree",
                table: "ParentSetMembers",
                columns: new[] { "ParentSetId", "ParentPersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParentSetMembers_TreeId",
                schema: "webtree",
                table: "ParentSetMembers",
                column: "TreeId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentSets_ChildPersonId",
                schema: "webtree",
                table: "ParentSets",
                column: "ChildPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentSets_TreeId",
                schema: "webtree",
                table: "ParentSets",
                column: "TreeId");

            // Existing parent links become one primary Birth set per child, then the legacy rows go.
            migrationBuilder.Sql(@"
                INSERT INTO `webtree`.`ParentSets` (`TreeId`, `ChildPersonId`, `Type`, `IsPrimary`)
                SELECT r.`TreeId`, r.`ToPersonId`, 0, 1
                FROM `webtree`.`Relationships` r
                WHERE r.`Kind` = 0
                GROUP BY r.`TreeId`, r.`ToPersonId`");

            migrationBuilder.Sql(@"
                INSERT INTO `webtree`.`ParentSetMembers` (`TreeId`, `ParentSetId`, `ParentPersonId`)
                SELECT r.`TreeId`, s.`Id`, r.`FromPersonId`
                FROM `webtree`.`Relationships` r
                INNER JOIN `webtree`.`ParentSets` s ON s.`TreeId` = r.`TreeId` AND s.`ChildPersonId` = r.`ToPersonId`
                WHERE r.`Kind` = 0");

            migrationBuilder.Sql(@"DELETE FROM `webtree`.`Relationships` WHERE `Kind` = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Put each parent back as a legacy parent link. Only a primary set existed as a link before, so the other sets are dropped with their tables.
            migrationBuilder.Sql(@"
                INSERT INTO `webtree`.`Relationships` (`TreeId`, `FromPersonId`, `ToPersonId`, `Kind`)
                SELECT m.`TreeId`, m.`ParentPersonId`, s.`ChildPersonId`, 0
                FROM `webtree`.`ParentSetMembers` m
                INNER JOIN `webtree`.`ParentSets` s ON s.`Id` = m.`ParentSetId`
                WHERE s.`IsPrimary` = 1");

            migrationBuilder.DropTable(
                name: "ParentSetMembers",
                schema: "webtree");

            migrationBuilder.DropTable(
                name: "ParentSets",
                schema: "webtree");
        }
    }
}
