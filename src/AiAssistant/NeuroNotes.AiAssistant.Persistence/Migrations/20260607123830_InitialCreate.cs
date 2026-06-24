using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NeuroNotes.AiAssistant.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "ai_assistant");

        migrationBuilder.CreateTable(
            name: "Notes",
            schema: "ai_assistant",
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
            schema: "ai_assistant",
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

        migrationBuilder.CreateIndex(
            name: "IX_Notes_UserId",
            schema: "ai_assistant",
            table: "Notes",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Tags_UserId_NormalizedName",
            schema: "ai_assistant",
            table: "Tags",
            columns: new[] { "UserId", "NormalizedName" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Notes",
            schema: "ai_assistant");

        migrationBuilder.DropTable(
            name: "Tags",
            schema: "ai_assistant");
    }
}