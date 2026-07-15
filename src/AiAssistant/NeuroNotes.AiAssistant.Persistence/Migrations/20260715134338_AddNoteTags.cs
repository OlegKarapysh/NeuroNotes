using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeuroNotes.AiAssistant.Persistence.Migrations;

/// <inheritdoc />
public partial class AddNoteTags : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "NoteTags",
            schema: "ai_assistant",
            columns: table => new
            {
                NoteId = table.Column<long>(type: "bigint", nullable: false),
                TagId = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NoteTags", x => new { x.NoteId, x.TagId });
                table.ForeignKey(
                    name: "FK_NoteTags_Notes_NoteId",
                    column: x => x.NoteId,
                    principalSchema: "ai_assistant",
                    principalTable: "Notes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_NoteTags_Tags_TagId",
                    column: x => x.TagId,
                    principalSchema: "ai_assistant",
                    principalTable: "Tags",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_NoteTags_TagId",
            schema: "ai_assistant",
            table: "NoteTags",
            column: "TagId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "NoteTags",
            schema: "ai_assistant");
    }
}