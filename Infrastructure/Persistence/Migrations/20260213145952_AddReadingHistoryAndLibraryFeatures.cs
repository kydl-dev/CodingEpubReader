using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReadingHistoryAndLibraryFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Libraries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TotalBooks = table.Column<int>(type: "INTEGER", nullable: false),
                    StoragePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Libraries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LibraryIndexes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BookId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Author = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    IndexedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastAccessedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AccessCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryIndexes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibraryIndexes_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReadingHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BookId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LastReadAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TotalReadingTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    TotalSessions = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadingHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReadingHistories_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavedCssStyles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FontFamily = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FontSize = table.Column<double>(type: "REAL", nullable: false),
                    LineHeight = table.Column<double>(type: "REAL", nullable: false),
                    LetterSpacing = table.Column<double>(type: "REAL", nullable: false),
                    MarginHorizontal = table.Column<int>(type: "INTEGER", nullable: false),
                    MarginVertical = table.Column<int>(type: "INTEGER", nullable: false),
                    CustomCss = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedCssStyles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Libraries_Name",
                table: "Libraries",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LibraryIndexes_Author",
                table: "LibraryIndexes",
                column: "Author");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryIndexes_BookId",
                table: "LibraryIndexes",
                column: "BookId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LibraryIndexes_IsFavorite",
                table: "LibraryIndexes",
                column: "IsFavorite");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryIndexes_LastAccessedAt",
                table: "LibraryIndexes",
                column: "LastAccessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryIndexes_Title",
                table: "LibraryIndexes",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_ReadingHistories_BookId",
                table: "ReadingHistories",
                column: "BookId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReadingHistories_LastReadAt",
                table: "ReadingHistories",
                column: "LastReadAt");

            migrationBuilder.CreateIndex(
                name: "IX_SavedCssStyles_IsDefault",
                table: "SavedCssStyles",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_SavedCssStyles_Name",
                table: "SavedCssStyles",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Libraries");

            migrationBuilder.DropTable(
                name: "LibraryIndexes");

            migrationBuilder.DropTable(
                name: "ReadingHistories");

            migrationBuilder.DropTable(
                name: "SavedCssStyles");
        }
    }
}
