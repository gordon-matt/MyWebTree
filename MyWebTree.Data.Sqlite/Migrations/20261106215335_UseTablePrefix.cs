using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyWebTree.Data.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class UseTablePrefix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParentSetMembers_ParentSets_ParentSetId",
                schema: "webtree",
                table: "ParentSetMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_ParentSetMembers_People_ParentPersonId",
                schema: "webtree",
                table: "ParentSetMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_ParentSets_People_ChildPersonId",
                schema: "webtree",
                table: "ParentSets");

            migrationBuilder.DropForeignKey(
                name: "FK_People_Trees_TreeId",
                schema: "webtree",
                table: "People");

            migrationBuilder.DropForeignKey(
                name: "FK_Relationships_People_FromPersonId",
                schema: "webtree",
                table: "Relationships");

            migrationBuilder.DropForeignKey(
                name: "FK_Relationships_People_ToPersonId",
                schema: "webtree",
                table: "Relationships");

            migrationBuilder.DropForeignKey(
                name: "FK_Trees_People_FocusPersonId",
                schema: "webtree",
                table: "Trees");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Trees",
                schema: "webtree",
                table: "Trees");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Relationships",
                schema: "webtree",
                table: "Relationships");

            migrationBuilder.DropPrimaryKey(
                name: "PK_People",
                schema: "webtree",
                table: "People");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ParentSets",
                schema: "webtree",
                table: "ParentSets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ParentSetMembers",
                schema: "webtree",
                table: "ParentSetMembers");

            migrationBuilder.RenameTable(
                name: "Trees",
                schema: "webtree",
                newName: "MyWebTree_Trees");

            migrationBuilder.RenameTable(
                name: "Relationships",
                schema: "webtree",
                newName: "MyWebTree_Relationships");

            migrationBuilder.RenameTable(
                name: "People",
                schema: "webtree",
                newName: "MyWebTree_People");

            migrationBuilder.RenameTable(
                name: "ParentSets",
                schema: "webtree",
                newName: "MyWebTree_ParentSets");

            migrationBuilder.RenameTable(
                name: "ParentSetMembers",
                schema: "webtree",
                newName: "MyWebTree_ParentSetMembers");

            migrationBuilder.RenameIndex(
                name: "IX_Trees_FocusPersonId",
                table: "MyWebTree_Trees",
                newName: "IX_MyWebTree_Trees_FocusPersonId");

            migrationBuilder.RenameIndex(
                name: "IX_Relationships_TreeId_Kind",
                table: "MyWebTree_Relationships",
                newName: "IX_MyWebTree_Relationships_TreeId_Kind");

            migrationBuilder.RenameIndex(
                name: "IX_Relationships_ToPersonId",
                table: "MyWebTree_Relationships",
                newName: "IX_MyWebTree_Relationships_ToPersonId");

            migrationBuilder.RenameIndex(
                name: "IX_Relationships_FromPersonId_ToPersonId_Kind",
                table: "MyWebTree_Relationships",
                newName: "IX_MyWebTree_Relationships_FromPersonId_ToPersonId_Kind");

            migrationBuilder.RenameIndex(
                name: "IX_People_TreeId",
                table: "MyWebTree_People",
                newName: "IX_MyWebTree_People_TreeId");

            migrationBuilder.RenameIndex(
                name: "IX_ParentSets_TreeId",
                table: "MyWebTree_ParentSets",
                newName: "IX_MyWebTree_ParentSets_TreeId");

            migrationBuilder.RenameIndex(
                name: "IX_ParentSets_ChildPersonId",
                table: "MyWebTree_ParentSets",
                newName: "IX_MyWebTree_ParentSets_ChildPersonId");

            migrationBuilder.RenameIndex(
                name: "IX_ParentSetMembers_TreeId",
                table: "MyWebTree_ParentSetMembers",
                newName: "IX_MyWebTree_ParentSetMembers_TreeId");

            migrationBuilder.RenameIndex(
                name: "IX_ParentSetMembers_ParentSetId_ParentPersonId",
                table: "MyWebTree_ParentSetMembers",
                newName: "IX_MyWebTree_ParentSetMembers_ParentSetId_ParentPersonId");

            migrationBuilder.RenameIndex(
                name: "IX_ParentSetMembers_ParentPersonId",
                table: "MyWebTree_ParentSetMembers",
                newName: "IX_MyWebTree_ParentSetMembers_ParentPersonId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MyWebTree_Trees",
                table: "MyWebTree_Trees",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MyWebTree_Relationships",
                table: "MyWebTree_Relationships",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MyWebTree_People",
                table: "MyWebTree_People",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MyWebTree_ParentSets",
                table: "MyWebTree_ParentSets",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MyWebTree_ParentSetMembers",
                table: "MyWebTree_ParentSetMembers",
                column: "Id");

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

            migrationBuilder.AddForeignKey(
                name: "FK_MyWebTree_Relationships_MyWebTree_People_FromPersonId",
                table: "MyWebTree_Relationships",
                column: "FromPersonId",
                principalTable: "MyWebTree_People",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MyWebTree_Relationships_MyWebTree_People_ToPersonId",
                table: "MyWebTree_Relationships",
                column: "ToPersonId",
                principalTable: "MyWebTree_People",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MyWebTree_Trees_MyWebTree_People_FocusPersonId",
                table: "MyWebTree_Trees",
                column: "FocusPersonId",
                principalTable: "MyWebTree_People",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MyWebTree_ParentSetMembers_MyWebTree_ParentSets_ParentSetId",
                table: "MyWebTree_ParentSetMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_MyWebTree_ParentSetMembers_MyWebTree_People_ParentPersonId",
                table: "MyWebTree_ParentSetMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_MyWebTree_ParentSets_MyWebTree_People_ChildPersonId",
                table: "MyWebTree_ParentSets");

            migrationBuilder.DropForeignKey(
                name: "FK_MyWebTree_People_MyWebTree_Trees_TreeId",
                table: "MyWebTree_People");

            migrationBuilder.DropForeignKey(
                name: "FK_MyWebTree_Relationships_MyWebTree_People_FromPersonId",
                table: "MyWebTree_Relationships");

            migrationBuilder.DropForeignKey(
                name: "FK_MyWebTree_Relationships_MyWebTree_People_ToPersonId",
                table: "MyWebTree_Relationships");

            migrationBuilder.DropForeignKey(
                name: "FK_MyWebTree_Trees_MyWebTree_People_FocusPersonId",
                table: "MyWebTree_Trees");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MyWebTree_Trees",
                table: "MyWebTree_Trees");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MyWebTree_Relationships",
                table: "MyWebTree_Relationships");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MyWebTree_People",
                table: "MyWebTree_People");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MyWebTree_ParentSets",
                table: "MyWebTree_ParentSets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MyWebTree_ParentSetMembers",
                table: "MyWebTree_ParentSetMembers");

            migrationBuilder.EnsureSchema(
                name: "webtree");

            migrationBuilder.RenameTable(
                name: "MyWebTree_Trees",
                newName: "Trees",
                newSchema: "webtree");

            migrationBuilder.RenameTable(
                name: "MyWebTree_Relationships",
                newName: "Relationships",
                newSchema: "webtree");

            migrationBuilder.RenameTable(
                name: "MyWebTree_People",
                newName: "People",
                newSchema: "webtree");

            migrationBuilder.RenameTable(
                name: "MyWebTree_ParentSets",
                newName: "ParentSets",
                newSchema: "webtree");

            migrationBuilder.RenameTable(
                name: "MyWebTree_ParentSetMembers",
                newName: "ParentSetMembers",
                newSchema: "webtree");

            migrationBuilder.RenameIndex(
                name: "IX_MyWebTree_Trees_FocusPersonId",
                schema: "webtree",
                table: "Trees",
                newName: "IX_Trees_FocusPersonId");

            migrationBuilder.RenameIndex(
                name: "IX_MyWebTree_Relationships_TreeId_Kind",
                schema: "webtree",
                table: "Relationships",
                newName: "IX_Relationships_TreeId_Kind");

            migrationBuilder.RenameIndex(
                name: "IX_MyWebTree_Relationships_ToPersonId",
                schema: "webtree",
                table: "Relationships",
                newName: "IX_Relationships_ToPersonId");

            migrationBuilder.RenameIndex(
                name: "IX_MyWebTree_Relationships_FromPersonId_ToPersonId_Kind",
                schema: "webtree",
                table: "Relationships",
                newName: "IX_Relationships_FromPersonId_ToPersonId_Kind");

            migrationBuilder.RenameIndex(
                name: "IX_MyWebTree_People_TreeId",
                schema: "webtree",
                table: "People",
                newName: "IX_People_TreeId");

            migrationBuilder.RenameIndex(
                name: "IX_MyWebTree_ParentSets_TreeId",
                schema: "webtree",
                table: "ParentSets",
                newName: "IX_ParentSets_TreeId");

            migrationBuilder.RenameIndex(
                name: "IX_MyWebTree_ParentSets_ChildPersonId",
                schema: "webtree",
                table: "ParentSets",
                newName: "IX_ParentSets_ChildPersonId");

            migrationBuilder.RenameIndex(
                name: "IX_MyWebTree_ParentSetMembers_TreeId",
                schema: "webtree",
                table: "ParentSetMembers",
                newName: "IX_ParentSetMembers_TreeId");

            migrationBuilder.RenameIndex(
                name: "IX_MyWebTree_ParentSetMembers_ParentSetId_ParentPersonId",
                schema: "webtree",
                table: "ParentSetMembers",
                newName: "IX_ParentSetMembers_ParentSetId_ParentPersonId");

            migrationBuilder.RenameIndex(
                name: "IX_MyWebTree_ParentSetMembers_ParentPersonId",
                schema: "webtree",
                table: "ParentSetMembers",
                newName: "IX_ParentSetMembers_ParentPersonId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Trees",
                schema: "webtree",
                table: "Trees",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Relationships",
                schema: "webtree",
                table: "Relationships",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_People",
                schema: "webtree",
                table: "People",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ParentSets",
                schema: "webtree",
                table: "ParentSets",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ParentSetMembers",
                schema: "webtree",
                table: "ParentSetMembers",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ParentSetMembers_ParentSets_ParentSetId",
                schema: "webtree",
                table: "ParentSetMembers",
                column: "ParentSetId",
                principalSchema: "webtree",
                principalTable: "ParentSets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ParentSetMembers_People_ParentPersonId",
                schema: "webtree",
                table: "ParentSetMembers",
                column: "ParentPersonId",
                principalSchema: "webtree",
                principalTable: "People",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ParentSets_People_ChildPersonId",
                schema: "webtree",
                table: "ParentSets",
                column: "ChildPersonId",
                principalSchema: "webtree",
                principalTable: "People",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_People_Trees_TreeId",
                schema: "webtree",
                table: "People",
                column: "TreeId",
                principalSchema: "webtree",
                principalTable: "Trees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Relationships_People_FromPersonId",
                schema: "webtree",
                table: "Relationships",
                column: "FromPersonId",
                principalSchema: "webtree",
                principalTable: "People",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Relationships_People_ToPersonId",
                schema: "webtree",
                table: "Relationships",
                column: "ToPersonId",
                principalSchema: "webtree",
                principalTable: "People",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Trees_People_FocusPersonId",
                schema: "webtree",
                table: "Trees",
                column: "FocusPersonId",
                principalSchema: "webtree",
                principalTable: "People",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
