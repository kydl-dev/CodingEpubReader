# CodingEpubReader

A desktop EPUB reader built with Avalonia and a Clean Architecture-style .NET solution.

## What it can do right now

### Library management
- Scan configured folders and import `.epub` files.
- Detect duplicates during import (ISBN/UUID/title+author matching).
- Show your library with title, author, language, added date, and last opened date.
- Show cover previews (when available).
- Open a details modal with metadata (publisher, ISBN, subject, rights, EPUB version, description).
- Delete a book from the library (also removes associated bookmarks/highlights/progress data for that book).
- Keep reading history snapshots even after a book is deleted.

### Reading experience
- Open EPUB books and navigate by table of contents.
- Render chapter HTML in an embedded WebView.
- Persist reading progress and restore it when reopening.
- Search inside a book, including:
  - search suggestions
  - case-sensitive mode
  - whole-word mode
  - jump-to-result with in-page highlight focus
- Copy current chapter plain text to clipboard.
- Export full book HTML to clipboard.

### Themes and styles
- Switch application theme: `Dark`, `Light`, `Sepia`.
- Switch reader CSS style independently from app chrome.
- Persist theme/style defaults in the database.

### Recently read
- View recently read entries with:
  - last read timestamp
  - total sessions
  - cumulative reading time
- Reopen books directly from the recently read list.

### Admin panel and diagnostics
- Cache dashboard:
  - total cached items
  - key prefixes
  - full key list
  - clear cache
- Run background workers manually:
  - library scanning
  - database maintenance
  - cover cache generation
  - logging statistics aggregation
  - reading session tracking
- View worker configuration snapshots and latest run times.
- Inspect logging stats (top errors, hourly series, totals).
- See active reading sessions.

## Architecture (current)

- `DesktopUI`: Avalonia UI (views + viewmodels)
- `Application`: use cases / commands / queries
- `Domain`: entities, value objects, repository contracts
- `Infrastructure`: EF Core, SQLite, EPUB parsing, services
- `Shared`: worker abstractions, common utilities, exceptions

## Tech stack

- .NET `10.0`
- Avalonia UI `11.x`
- ReactiveUI
- MediatR
- Entity Framework Core + SQLite
- Serilog
- `VersOne.Epub` for EPUB parsing

## Run locally

### Prerequisites
- .NET SDK compatible with `global.json` (`10.0.0` configured)
- On Windows, WebView2 runtime is recommended for embedded rendering

### Start the app

```powershell
dotnet run --project DesktopUI/DesktopUI.csproj
```

## Configuration

Main settings file:
- `DesktopUI/appsettings.json`

Notable sections:
- `ConnectionStrings:DefaultConnection`
- `BackgroundWorkers:*`
- `FileStorage:*`
- `Application:EnableBackgroundWorkers`

## Current status notes

- Backend support exists for bookmarks/highlights and related use cases.
- The current desktop UI is primarily focused on library browsing, reading, search, themes, and admin tooling.
