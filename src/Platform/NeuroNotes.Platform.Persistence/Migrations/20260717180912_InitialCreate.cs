using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NeuroNotes.Platform.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "platform");

        migrationBuilder.CreateTable(
            name: "Bots",
            schema: "platform",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                TelegramBotId = table.Column<long>(type: "bigint", nullable: false),
                Username = table.Column<string>(type: "text", nullable: true),
                Label = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                BehaviorKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                EncryptedToken = table.Column<byte[]>(type: "bytea", nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Bots", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "DataProtectionKeys",
            schema: "platform",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                FriendlyName = table.Column<string>(type: "text", nullable: true),
                Xml = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Bots_TelegramBotId",
            schema: "platform",
            table: "Bots",
            column: "TelegramBotId",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Bots",
            schema: "platform");

        migrationBuilder.DropTable(
            name: "DataProtectionKeys",
            schema: "platform");
    }
}