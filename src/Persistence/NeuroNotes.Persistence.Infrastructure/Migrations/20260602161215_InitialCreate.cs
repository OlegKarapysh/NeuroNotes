using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NeuroNotes.Persistence.Infrastructure.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Notes",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                UserId = table.Column<long>(type: "bigint", nullable: false),
                FileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                Content = table.Column<string>(type: "text", nullable: false),
                SavedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Notes", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Tags",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                UserId = table.Column<long>(type: "bigint", nullable: false),
                Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                NormalizedName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Tags", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "UserGitHubSettings",
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

        migrationBuilder.CreateIndex(
            name: "IX_Notes_UserId",
            table: "Notes",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Tags_UserId_NormalizedName",
            table: "Tags",
            columns: new[] { "UserId", "NormalizedName" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Notes");

        migrationBuilder.DropTable(
            name: "Tags");

        migrationBuilder.DropTable(
            name: "UserGitHubSettings");
    }
}