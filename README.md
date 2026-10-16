# TextFlow Pro 📝

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![Windows](https://img.shields.io/badge/Platform-Windows%2011%20%7C%2010-0078D6?style=flat&logo=windows)](https://www.microsoft.com/windows)
[![WinUI 3](https://img.shields.io/badge/UI-WinUI%203%20%2F%20Windows%20App%20SDK-0078D7?style=flat)](https://learn.microsoft.com/windows/apps/winui/winui3/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Build & Test](https://github.com/your-username/TextFlow/actions/workflows/ci.yml/badge.svg)](https://github.com/your-username/TextFlow/actions/workflows/ci.yml)

**TextFlow Pro** is a modern, lightweight, and extensible text editor designed for Windows 11. Built with **WinUI 3**, **Windows App SDK**, **.NET 9**, and **C#**, it follows **Clean Architecture**, **MVVM pattern**, and native Windows 11 Fluent Design principles with Mica material.

---

## ✨ Features

### 🗂️ Multi-Tab Workspace
- **Drag-and-Drop Tabs**: Open, reorder, and drag tabs smoothly using Windows App SDK `TabView`.
- **Intelligent Untitled Naming**: New unsaved tabs automatically infer their display title from the first non-empty line of text.
- **Visual Modification Markers**: Unsaved documents clearly show a status indicator (`*`).
- **Shortcuts & Tab Controls**:
  - `Ctrl + T` / `Ctrl + N`: New tab / New document
  - `Ctrl + W`: Close current tab (also supports middle-click on tab)
  - `Ctrl + Shift + N`: Launch a new editor window
  - `Ctrl + Tab` / `Ctrl + Shift + Tab`: Cycle between open tabs

### 🎨 Windows 11 Fluent Design & Custom Titlebar
- **Integrated Modern Titlebar**: Custom borderless titlebar blending seamlessly with the menu bar and window caption buttons.
- **Mica Material Backdrop**: Dynamic subtle translucent window backdrop honoring system light and dark themes.
- **Consolas Typography**: Clean, legible monospace editing with aligned line numbering.
- **Window State Persistence**: Remembers exact window dimensions, screen coordinates, and maximized state across sessions.

### 🔍 Powerful Find & Replace
- Floating docked search overlay (`Ctrl + F`, `Ctrl + H`).
- Search options:
  - Case sensitive (`Aa`)
  - Whole word matching (`[ab]`)
  - Regular expressions (`.*`)
- Next / Previous navigation and instant Replace / Replace All.

### 💾 Auto-Save & Crash Recovery
- **Automatic Background Backup**: Silently saves document snapshots at configurable intervals (default: 30 seconds).
- **Crash Recovery Dialog**: Prompts on startup if an unexpected shutdown or crash is detected, allowing one-click restore or discard of unsaved backups.

### ☁️ Cloud Sync (Google Drive)
- Native OAuth 2.0 authentication and Google Drive API integration.
- Offline-first background sync queue powered by SQLite.
- Status bar sync indicators (`Synced ✓`, `Syncing...`, `Offline`).

### ⚖️ Document Comparison
- Side-by-side visual diff tool showing added, removed, and modified line statistics.

---

## 🏗️ Architecture & Project Structure

The project strictly follows Clean Architecture and SOLID principles:

```
TextFlow/
├── .github/workflows/                 # Continuous Integration pipelines
├── src/
│   ├── TextFlow.Core/                 # Domain, services, business logic, MVVM
│   │   ├── Infrastructure/            # Encoding detection & enum types
│   │   ├── Models/                    # Document, Session, Settings, SyncJob models
│   │   ├── Persistence/               # SQLite database service
│   │   ├── Services/                  # File, AutoSave, Recovery, Session, GoogleDrive, AI services
│   │   └── ViewModels/                # MainViewModel, DocumentViewModel, SettingsViewModel, etc.
│   │
│   └── TextFlow/                      # WinUI 3 Desktop presentation layer
│       ├── App.xaml / .cs             # Dependency Injection & Serilog logging
│       ├── MainWindow.xaml / .cs      # Fluent UI, integrated titlebar, editor
│       ├── Views/                     # Settings, Recovery, GoogleDrive, Compare dialogs
│       └── Assets/                    # High-DPI icons and branding assets
│
└── tests/
    └── TextFlow.Tests/                # 40 automated xUnit tests
        ├── FileEncodingDetectorTests.cs
        ├── FileServiceTests.cs
        ├── EditorServiceTests.cs
        ├── DatabaseAndAutoSaveTests.cs
        ├── DocumentCompareViewModelTests.cs
        └── SettingsAndSessionTests.cs
```

---

## 🛠️ Getting Started

### Prerequisites
- **OS**: Windows 10 (version 1809 or higher) or Windows 11
- **SDK**: [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Workload**: Windows App SDK (WinUI 3)

### Build Solution
```powershell
dotnet build TextFlow.sln
```

### Run Unit Tests
```powershell
dotnet test TextFlow.sln
```

### Run Application in Development
```powershell
dotnet run --project src/TextFlow/TextFlow.csproj
```

### Publish Standalone Release (No .NET installation required)
```powershell
dotnet publish src/TextFlow/TextFlow.csproj -c Release -r win-x64 -p:PublishProfile=win-x64
```
The published output will be generated in `src/TextFlow/bin/Release/net9.0-windows10.0.26100.0/win-x64/publish/` with all dependencies packaged side-by-side.

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).
