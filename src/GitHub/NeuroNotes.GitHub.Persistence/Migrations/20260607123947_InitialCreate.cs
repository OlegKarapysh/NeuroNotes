using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeuroNotes.GitHub.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "github");

        migrationBuilder.CreateTable(
            name: "UserGitHubSettings",
            schema: "github",
            columns: table => new
            {
                UserId = table.Column<long>(type: "bigint", nullable: false),
                Owner = table.Column<string>(type: "text", nullable: false),
                Repo = table.Column<string>(type: "text", nullable: false),
                Branch = table.Column<string>(type: "text", nullable: false),
                NotesFolder = table.Column<string>(type: "text", nullable: false),
                AccessToken = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserGitHubSettings", x => x.UserId);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "UserGitHubSettings",
            schema: "github");
    }
}