using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeuroNotes.AiAssistant.Persistence.Migrations;

/// <inheritdoc />
public partial class AddBotIdScoping : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Tags_UserId_NormalizedName",
            schema: "ai_assistant",
            table: "Tags");

        migrationBuilder.DropIndex(
            name: "IX_Notes_UserId",
            schema: "ai_assistant",
            table: "Notes");

        migrationBuilder.AddColumn<long>(
            name: "BotId",
            schema: "ai_assistant",
            table: "Tags",
            type: "bigint",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.AddColumn<long>(
            name: "BotId",
            schema: "ai_assistant",
            table: "Notes",
            type: "bigint",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.CreateIndex(
            name: "IX_Tags_BotId_UserId_NormalizedName",
            schema: "ai_assistant",
            table: "Tags",
            columns: new[] { "BotId", "UserId", "NormalizedName" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Notes_BotId_UserId",
            schema: "ai_assistant",
            table: "Notes",
            columns: new[] { "BotId", "UserId" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Tags_BotId_UserId_NormalizedName",
            schema: "ai_assistant",
            table: "Tags");

        migrationBuilder.DropIndex(
            name: "IX_Notes_BotId_UserId",
            schema: "ai_assistant",
            table: "Notes");

        migrationBuilder.DropColumn(
            name: "BotId",
            schema: "ai_assistant",
            table: "Tags");

        migrationBuilder.DropColumn(
            name: "BotId",
            schema: "ai_assistant",
            table: "Notes");

        migrationBuilder.CreateIndex(
            name: "IX_Tags_UserId_NormalizedName",
            schema: "ai_assistant",
            table: "Tags",
            columns: new[] { "UserId", "NormalizedName" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Notes_UserId",
            schema: "ai_assistant",
            table: "Notes",
            column: "UserId");
    }
}