using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeuroNotes.TelegramBot.Persistence.Migrations;

/// <inheritdoc />
public partial class AddBotIdScoping : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropPrimaryKey(
            name: "PK_LastTranscriptions",
            schema: "telegram_bot",
            table: "LastTranscriptions");

        migrationBuilder.DropPrimaryKey(
            name: "PK_ChatStates",
            schema: "telegram_bot",
            table: "ChatStates");

        migrationBuilder.AddColumn<long>(
            name: "BotId",
            schema: "telegram_bot",
            table: "LastTranscriptions",
            type: "bigint",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.AddColumn<long>(
            name: "BotId",
            schema: "telegram_bot",
            table: "ChatStates",
            type: "bigint",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.AddPrimaryKey(
            name: "PK_LastTranscriptions",
            schema: "telegram_bot",
            table: "LastTranscriptions",
            columns: new[] { "BotId", "ChatId" });

        migrationBuilder.AddPrimaryKey(
            name: "PK_ChatStates",
            schema: "telegram_bot",
            table: "ChatStates",
            columns: new[] { "BotId", "ChatId" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropPrimaryKey(
            name: "PK_LastTranscriptions",
            schema: "telegram_bot",
            table: "LastTranscriptions");

        migrationBuilder.DropPrimaryKey(
            name: "PK_ChatStates",
            schema: "telegram_bot",
            table: "ChatStates");

        migrationBuilder.DropColumn(
            name: "BotId",
            schema: "telegram_bot",
            table: "LastTranscriptions");

        migrationBuilder.DropColumn(
            name: "BotId",
            schema: "telegram_bot",
            table: "ChatStates");

        migrationBuilder.AddPrimaryKey(
            name: "PK_LastTranscriptions",
            schema: "telegram_bot",
            table: "LastTranscriptions",
            column: "ChatId");

        migrationBuilder.AddPrimaryKey(
            name: "PK_ChatStates",
            schema: "telegram_bot",
            table: "ChatStates",
            column: "ChatId");
    }
}