# Clipboard History Manager

A lightweight, keyboard-driven clipboard history manager for Windows that lives in your system tray and gives you instant access to everything you have copied.

### How It Works

Clipboard History Manager starts silently and settles into the system tray. It listens for native Windows clipboard change notifications through the `AddClipboardFormatListener` Win32 API, so every text snippet you copy from any application is captured in real time without polling. A global hotkey (`Ctrl + Shift + V`), registered through `RegisterHotKey`, summons a compact search window ranked with your pinned entries first, followed by the most recent items. Selecting an entry writes it back to the clipboard so you can paste it wherever you need. The list keeps the last 100 unpinned items and any number of pinned ones; pinned items are persisted to `%APPDATA%\ClipboardHistory\store.json` and reloaded on the next launch.

### Setup

**Requirements**
- Windows 10 or Windows 11
- .NET 8 SDK (for building from source)

**Build**
```bash
dotnet build Clipboard-History-Manager.csproj -c Release
```

The executable is produced at `bin\Release\net8.0-windows\Clipboard-History-Manager.exe`.

### Usage

1. Launch the app. It runs silently in the system tray (look for the blue clipboard icon near the clock).
2. Copy text from any application as you normally would.
3. Press `Ctrl + Shift + V` to open the history window.
4. Type in the search box to filter by content, or use the arrow keys to navigate.
5. Press `Enter` (or double-click) to copy an entry back to the clipboard.
6. Click the star icon on any entry to pin it so it survives across sessions.
7. Right-click an entry for Copy, Pin/Unpin, and Delete actions.
8. Press `Esc` to hide the window, or use the tray icon menu to `Exit`.

### Features

- Global hotkey (`Ctrl + Shift + V`) for instant access
- Real-time capture via native Windows clipboard listener (no polling)
- Fast case-insensitive live search
- Pin frequently used entries — persisted to disk and always shown first
- Right-click context menu with Copy, Pin/Unpin, Delete
- Clean, modern dark theme with fully custom controls
- System tray operation with hide-on-close behavior
- Single-instance enforcement via named mutex
- Automatic cap of 100 unpinned entries to keep memory tidy
- Duplicate detection — re-copying an existing entry promotes it instead of duplicating
- Escape to hide, Enter to paste, Delete to remove
- Zero external UI dependencies — pure WPF with hand-crafted styles
