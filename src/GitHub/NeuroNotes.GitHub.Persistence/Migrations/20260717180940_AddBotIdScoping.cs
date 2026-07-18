using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeuroNotes.GitHub.Persistence.Migrations;

/// <inheritdoc />
public partial class AddBotIdScoping : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropPrimaryKey(
            name: "PK_UserGitHubSettings",
            schema: "github",
            table: "UserGitHubSettings");

        migrationBuilder.AddColumn<long>(
            name: "BotId",
            schema: "github",
            table: "UserGitHubSettings",
            type: "bigint",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.AddPrimaryKey(
            name: "PK_UserGitHubSettings",
            schema: "github",
            table: "UserGitHubSettings",
            columns: new[] { "BotId", "UserId" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropPrimaryKey(
            name: "PK_UserGitHubSettings",
            schema: "github",
            table: "UserGitHubSettings");

        migrationBuilder.DropColumn(
            name: "BotId",
            schema: "github",
            table: "UserGitHubSettings");

        migrationBuilder.AddPrimaryKey(
            name: "PK_UserGitHubSettings",
            schema: "github",
            table: "UserGitHubSettings",
            column: "UserId");
    }
}