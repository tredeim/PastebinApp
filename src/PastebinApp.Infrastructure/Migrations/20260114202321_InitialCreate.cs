using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PastebinApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "paste_hashes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    hash = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    is_used = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paste_hashes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pastes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    hash = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    content_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    view_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    language = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pastes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_paste_hashes_hash",
                table: "paste_hashes",
                column: "hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_paste_hashes_is_used",
                table: "paste_hashes",
                column: "is_used");

            migrationBuilder.CreateIndex(
                name: "ix_paste_hashes_is_used_id",
                table: "paste_hashes",
                columns: new[] { "is_used", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_pastes_created_at",
                table: "pastes",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_pastes_expires_at",
                table: "pastes",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_pastes_expires_at_id",
                table: "pastes",
                columns: new[] { "expires_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_pastes_hash",
                table: "pastes",
                column: "hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "paste_hashes");

            migrationBuilder.DropTable(
                name: "pastes");
        }
    }
}
