using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bookmarks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BookId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ChapterId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PositionBookId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PositionSavedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Progress = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookmarks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Books",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Authors = table.Column<string>(type: "TEXT", nullable: false),
                    Language = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Metadata_Publisher = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Metadata_Description = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata_Creators = table.Column<string>(type: "TEXT", nullable: false),
                    Metadata_Isbn = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Metadata_GoogleBooksId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Metadata_CalibreId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Metadata_Uuid = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Metadata_Subject = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Metadata_Rights = table.Column<string>(type: "TEXT", nullable: true),
                    Metadata_PublishedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Metadata_CoverImagePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Metadata_Format = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Metadata_EpubVersion = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    AddedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastOpenedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Chapters = table.Column<string>(type: "TEXT", nullable: false),
                    TableOfContents = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Books", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Highlights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BookId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChapterId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StartOffset = table.Column<int>(type: "INTEGER", nullable: false),
                    EndOffset = table.Column<int>(type: "INTEGER", nullable: false),
                    SelectedText = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Highlights", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReadingPositions",
                columns: table => new
                {
                    BookId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChapterId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Progress = table.Column<double>(type: "REAL", nullable: false),
                    SavedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadingPositions", x => x.BookId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_BookId",
                table: "Bookmarks",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_BookId_Type",
                table: "Bookmarks",
                columns: new[] { "BookId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_CreatedAt",
                table: "Bookmarks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Books_AddedDate",
                table: "Books",
                column: "AddedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Books_LastOpenedDate",
                table: "Books",
                column: "LastOpenedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Books_Title",
                table: "Books",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_Highlights_BookId",
                table: "Highlights",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_Highlights_BookId_Color",
                table: "Highlights",
                columns: new[] { "BookId", "Color" });

            migrationBuilder.CreateIndex(
                name: "IX_Highlights_ChapterId",
                table: "Highlights",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Highlights_CreatedAt",
                table: "Highlights",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReadingPositions_SavedAt",
                table: "ReadingPositions",
                column: "SavedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bookmarks");

            migrationBuilder.DropTable(
                name: "Books");

            migrationBuilder.DropTable(
                name: "Highlights");

            migrationBuilder.DropTable(
                name: "ReadingPositions");
        }
    }
}
