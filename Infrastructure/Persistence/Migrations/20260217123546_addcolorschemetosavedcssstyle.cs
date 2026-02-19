using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{/// <inheritdoc />
    public partial class _20260214150000AddColorSchemeToSavedCssStyle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // These columns were part of the CssStyle.Colors owned-type that was
            // added to SavedCssStyle but the migration body was left empty.
            // Defaults match CssStyle.LightColorScheme so existing rows get a
            // sensible value without needing a data migration.
            migrationBuilder.AddColumn<string>(
                name: "ColorBackground",
                table: "SavedCssStyles",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "#FFFFFF");

            migrationBuilder.AddColumn<string>(
                name: "ColorBorder",
                table: "SavedCssStyles",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "#E0E0E0");

            migrationBuilder.AddColumn<string>(
                name: "ColorLink",
                table: "SavedCssStyles",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "#0066CC");

            migrationBuilder.AddColumn<string>(
                name: "ColorSelection",
                table: "SavedCssStyles",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "#B4D5FF");

            migrationBuilder.AddColumn<string>(
                name: "ColorSurface",
                table: "SavedCssStyles",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "#F5F5F5");

            migrationBuilder.AddColumn<string>(
                name: "ColorText",
                table: "SavedCssStyles",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "#1A1A1A");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ColorBackground", table: "SavedCssStyles");
            migrationBuilder.DropColumn(name: "ColorBorder",     table: "SavedCssStyles");
            migrationBuilder.DropColumn(name: "ColorLink",       table: "SavedCssStyles");
            migrationBuilder.DropColumn(name: "ColorSelection",  table: "SavedCssStyles");
            migrationBuilder.DropColumn(name: "ColorSurface",    table: "SavedCssStyles");
            migrationBuilder.DropColumn(name: "ColorText",       table: "SavedCssStyles");
        }
    }
}
