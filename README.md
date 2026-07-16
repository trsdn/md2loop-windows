<p align="center">
  <img src="assets/logo.svg" width="128" alt="md2loop logo">
</p>

<h1 align="center">md2loop</h1>

<p align="center">
  A lightweight Windows utility that converts Markdown ↔ Rich Text for seamless pasting into Microsoft Loop.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/License-MIT-blue.svg" alt="License: MIT">
  <img src="https://img.shields.io/badge/Platform-Windows%2010%2B-0078d4.svg" alt="Platform: Windows">
  <img src="https://img.shields.io/badge/.NET-8.0-512bd4.svg" alt=".NET 8">
  <img src="https://img.shields.io/badge/UI-WinUI%203-6c63ff.svg" alt="WinUI 3">
  <img src="https://github.com/trsdn/md2loop-windows/actions/workflows/build.yml/badge.svg" alt="Build">
  <img src="https://img.shields.io/github/v/release/trsdn/md2loop-windows" alt="Release">
</p>

---

Windows port of [md2loop](https://github.com/trsdn/md2loop) (originally macOS/Swift).

## Features

- **Markdown → Rich Text** — Converts clipboard Markdown or plain text to optimized HTML for pasting into Loop
- **Rich Text → Markdown** — Converts HTML or RTF from Loop, Office, and other apps back to clean Markdown
- **Smarter auto-detection** — Distinguishes semantic rich text from syntax-colored Markdown copies
- **Manual output controls** — Always choose **Rich Text** or **Markdown** yourself
- **One shortcut** — `Ctrl+Enter` uses the detected direction
- **Full Markdown support** — Headings, bold/italic, lists, tables, code blocks, task lists, links, blockquotes
- **Native Windows app** — WinUI 3, Mica backdrop, dark mode

## Installation

### Download (recommended)

Grab the latest release from the [Releases page](https://github.com/trsdn/md2loop-windows/releases):

| File | Description |
|------|-------------|
| `md2loop-setup-x.y.z-win-x64.exe` | **Installer** — Intel/AMD 64-bit |
| `md2loop-setup-x.y.z-win-arm64.exe` | **Installer** — ARM64 (Surface Pro, Snapdragon) |
| `md2loop-win-x64.zip` | Portable ZIP (Intel/AMD 64-bit) |
| `md2loop-win-arm64.zip` | Portable ZIP (ARM64 — Surface Pro, Snapdragon) |

The installer handles everything — just run it. No .NET runtime needed.

### Build from source

```powershell
git clone https://github.com/trsdn/md2loop-windows.git
cd md2loop-windows\md2loop
dotnet build
dotnet run
```

### Publish single-file executable

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o publish
```

## Usage

1. Copy Markdown text to your clipboard
2. Open md2loop — it recommends a conversion direction
3. Click **Rich Text** to format the clipboard for Loop, or press `Ctrl+Enter`
4. Paste into Microsoft Loop (`Ctrl+V`)

Works the other way too — copy rich text from Loop or Office and click **Markdown**. The two buttons remain available even when you want to override auto-detection.

## How it works

| Component | Purpose |
|-----------|---------|
| `ClipboardContentDetector.cs` | Scored detection of Markdown/plain text vs HTML/RTF |
| `ClipboardManager.cs` | Reads HTML, RTF, and text; writes HTML and text clipboard formats |
| `LoopHtmlConverter.cs` | Markdown → Loop-optimized HTML via [Markdig](https://github.com/xoofx/markdig) |
| `HtmlToMarkdownConverter.cs` | HTML → Markdown via [HtmlAgilityPack](https://html-agility-pack.net/) |
| `RtfToMarkdownConverter.cs` | RTF → HTML → Markdown via [RtfPipe](https://github.com/erdomke/RtfPipe) |
| `MainPage.xaml(.cs)` | WinUI 3 UI with explicit output buttons + Ctrl+Enter shortcut |

### Loop-specific optimizations

- No CSS classes in output HTML
- Unicode checkboxes (☑/☐) for task lists
- Minimal table HTML
- Multi-format clipboard (HTML + plain text) for maximum compatibility

## Requirements

- Windows 10 version 1809 or later
- No runtime dependencies when using the release build (self-contained)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) only needed for building from source

## Contributing

Contributions are welcome! Please open an issue or PR.

## License

MIT — see [LICENSE](LICENSE) for details.
