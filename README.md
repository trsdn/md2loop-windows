<h1 align="center">md2loop (Windows)</h1>

<p align="center">
  A lightweight Windows utility that converts Markdown ↔ Rich Text for seamless pasting into Microsoft Loop.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/License-MIT-blue.svg" alt="License: MIT">
  <img src="https://img.shields.io/badge/Platform-Windows%2010%2B-lightgrey.svg" alt="Platform: Windows">
  <img src="https://img.shields.io/badge/.NET-8.0-purple.svg" alt=".NET 8">
  <img src="https://img.shields.io/badge/UI-WinUI%203-blue.svg" alt="WinUI 3">
</p>

---

Windows port of [md2loop](https://github.com/trsdn/md2loop) (originally macOS/Swift).

## Features

- **Markdown → Loop** — Converts clipboard Markdown to optimized HTML for pasting into Loop
- **Loop → Markdown** — Converts Loop's rich text back to clean Markdown
- **Auto-detection** — Automatically detects content type (Markdown vs HTML)
- **One shortcut** — Ctrl+Enter does the right thing based on content
- **Full Markdown support** — Headings, bold/italic, lists, tables, code blocks, task lists, links, blockquotes
- **Native Windows app** — WinUI 3, lightweight, Mica backdrop

## Requirements

- Windows 10 version 1809 or later
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (for building)
- Windows App SDK runtime (bundled or installed separately)

## Build from source

```powershell
git clone https://github.com/trsdn/md2loop-windows.git
cd md2loop-windows\md2loop
dotnet build
```

## Run

```powershell
dotnet run
```

## Usage

1. Copy Markdown text to your clipboard
2. Open md2loop — it auto-detects the content type
3. Click **Convert to Loop** (or press Ctrl+Enter)
4. Paste into Microsoft Loop (Ctrl+V)

Works the other way too — copy from Loop, convert to Markdown.

## Architecture

| File | Purpose |
|------|---------|
| `ClipboardContentDetector.cs` | Regex-based detection of Markdown vs HTML content |
| `ClipboardManager.cs` | Windows clipboard read/write (HTML + text formats) |
| `LoopHtmlConverter.cs` | Markdown → Loop-optimized HTML (via Markdig) |
| `HtmlToMarkdownConverter.cs` | HTML → Markdown (via HtmlAgilityPack) |
| `MainPage.xaml(.cs)` | WinUI 3 UI with polling + keyboard shortcut |

## Dependencies

- [Markdig](https://github.com/xoofx/markdig) — Markdown parsing and HTML rendering
- [HtmlAgilityPack](https://html-agility-pack.net/) — HTML parsing for reverse conversion
- [Microsoft.WindowsAppSDK](https://github.com/microsoft/WindowsAppSDK) — WinUI 3

## License

MIT — see [LICENSE](LICENSE) for details.
