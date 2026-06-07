using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeuroNotes.TelegramBot.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "telegram_bot");

        migrationBuilder.CreateTable(
            name: "ChatStates",
            schema: "telegram_bot",
            columns: table => new
            {
                ChatId = table.Column<long>(type: "bigint", nullable: false),
                State = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ChatStates", x => x.ChatId);
            });

        migrationBuilder.CreateTable(
            name: "LastTranscriptions",
            schema: "telegram_bot",
            columns: table => new
            {
                ChatId = table.Column<long>(type: "bigint", nullable: false),
                Transcription = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LastTranscriptions", x => x.ChatId);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ChatStates",
            schema: "telegram_bot");

        migrationBuilder.DropTable(
            name: "LastTranscriptions",
            schema: "telegram_bot");
    }
}