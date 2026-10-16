# TextFlow Pro - Complete Prompt for Antigravity

Create a production-ready Windows desktop text editor application named **TextFlow**, developed using **WinUI 3**, **.NET 8**, and **C#**, following the **MVVM architecture pattern** with Dependency Injection and modern Fluent Design principles from Windows 11.

The application should function similarly to Windows 11 Notepad while providing advanced features including multi-tab document editing, Google Drive synchronization, automatic backup and recovery, session persistence, find and replace functionality, customizable editor appearance, and support for multiple text file formats.

## Technical Stack

- .NET 8
- WinUI 3 (Windows App SDK)
- C#
- MVVM Architecture
- Microsoft.Extensions.DependencyInjection
- SQLite
- System.Text.Json
- Serilog
- Google Drive API v3
- Fluent Design System

## Functional Requirements

### Multi-Tab Document Editor

Implement a tabbed document interface allowing users to open, edit, and manage multiple files simultaneously.

Features:

- Open multiple files in separate tabs
- Create new documents
- Close individual tabs
- Reorder tabs via drag-and-drop
- Display modified indicator (*) on unsaved tabs
- Middle-click tab closing
- Restore tabs after application restart
- Display file icon and filename on tab header
- Support Ctrl+Tab navigation between tabs

### Supported File Types

Support viewing, editing, saving, and syncing the following file formats:

- .txt
- .md
- .json
- .csv
- .sql

Supported text encodings:

- UTF-8
- UTF-8 with BOM
- UTF-16
- ASCII

Automatically detect encoding when opening files.

### Text Editing Features

Implement a professional text editing experience including:

- Undo
- Redo
- Copy
- Paste
- Cut
- Select All
- Word Wrap
- Line Numbers
- Current Line Highlight
- Zoom In
- Zoom Out
- Keyboard shortcuts
- Drag-and-drop file opening

Keyboard shortcuts:

| Action | Shortcut |
|----------|-----------|
| New File | Ctrl+N |
| New Tab | Ctrl+T |
| Open File | Ctrl+O |
| Save | Ctrl+S |
| Save As | Ctrl+Shift+S |
| Save All | Ctrl+Shift+A |
| Close Tab | Ctrl+W |
| Find | Ctrl+F |
| Replace | Ctrl+H |
| Next Tab | Ctrl+Tab |
| Previous Tab | Ctrl+Shift+Tab |

### Editor Appearance Customization

Allow users to configure editor appearance globally and persist settings between sessions.

Font Family examples:

- Cascadia Code
- Consolas
- Segoe UI
- Courier New

Font Styles:

- Regular
- Bold
- Italic
- Bold Italic

Font Size:

- Minimum 8
- Maximum 72

Store preferences in user configuration.

## Find And Replace

Implement an advanced Find & Replace module.

Find Features:

- Incremental search
- Highlight all matches
- Match Case
- Whole Word
- Regular Expression support

Replace Features:

- Replace current match
- Replace all matches
- Preview replacement results

Access through:

- Ctrl+F
- Ctrl+H

## Auto Save System

Implement an automatic background save system for crash protection.

Every 30 seconds:

- Save modified documents to temporary storage
- Create backup version
- Maintain autosave history

Autosave path:

```text
%TEMP%\TextFlowPro\
```

When application starts:

- Detect autosave sessions
- Display recovery screen
- Allow restoring or discarding autosaved files

## Google Drive Integration

Implement native integration with Google Drive using OAuth 2.0 and Google Drive API v3.

Authentication:

- Sign In with Google
- Sign Out
- Token refresh management
- Secure credential storage

Synchronization:

- Upload modified files automatically
- Download latest cloud version
- Background synchronization
- Sync status display
- Manual sync button

Supported Sync Modes:

### Auto Sync

Trigger synchronization when:

- Document saved
- Auto-save executed
- File content changed
- Application closing

### Manual Sync

User presses Sync button.

### Offline Mode

When internet is unavailable:

- Continue editing locally
- Queue sync jobs
- Retry synchronization automatically

### Conflict Resolution

Provide options:

- Local Version Wins
- Cloud Version Wins
- Compare Changes
- Merge Manually

Google Drive settings must allow users to:

- Select sync folder
- Configure sync interval
- Enable/disable auto sync

## Session Persistence

The application must automatically remember and restore the user's workspace.

Save on Exit:

- Open document list
- Active tab
- Cursor position
- Scroll position
- Window size
- Window location
- Theme
- Font configuration
- Google Drive status

Store session data in:

```text
%LOCALAPPDATA%\TextFlowPro\
```

Example:

```json
{
  "openedFiles": [],
  "activeFile": "",
  "windowWidth": 1600,
  "windowHeight": 900,
  "theme": "Dark",
  "fontFamily": "Cascadia Code",
  "fontSize": 14
}
```

On next application startup:

- Restore files
- Restore tabs
- Restore editor state
- Restore cursor locations
- Restore active tab

## User Interface

Follow Windows 11 Fluent Design guidelines.

Support:

- Light Theme
- Dark Theme
- System Theme

Layout:

```text
+------------------------------------------------------+
| Menu Bar                                              |
+------------------------------------------------------+
| Toolbar                                               |
+------------------------------------------------------+
| Multi-Tab Area                                        |
+------------------------------------------------------+
|                                                      |
|                  Editor Content                      |
|                                                      |
+------------------------------------------------------+
| Status Bar                                            |
+------------------------------------------------------+
```

### Menu Bar

File:

- New
- Open
- Save
- Save As
- Save All
- Close Tab
- Exit

Edit:

- Undo
- Redo
- Cut
- Copy
- Paste
- Find
- Replace

View:

- Theme
- Word Wrap
- Line Numbers
- Zoom

Tools:

- Sync
- Google Drive Settings
- Preferences

Help:

- About

### Toolbar

Include:

- New
- Open
- Save
- Save All
- Undo
- Redo
- Find
- Replace
- Sync
- Settings

### Status Bar

Display:

- Current file type
- Encoding
- Cursor position
- Line number
- Column number
- Modified status
- Sync status

Example:

```text
UTF-8 | Line 25 | Column 10 | Synced ✓
```

## Settings Module

### General Settings

- Auto-save interval
- Startup behavior
- Default encoding
- Recent file count

### Editor Settings

- Font family
- Font size
- Font style
- Word wrap
- Line numbers
- Current line highlight

### Theme Settings

- Light
- Dark
- System

### Google Drive Settings

- Login
- Logout
- Sync directory
- Sync interval
- Auto sync toggle

## Performance Requirements

Startup:

```text
Less than 2 seconds
```

File Open:

```text
Less than 500 ms for files under 10 MB
```

Memory Usage:

```text
Below 300 MB during typical editing
```

Large File Support:

```text
100+ MB text files
```

Prevent UI freezing by using asynchronous operations throughout the application.

## Recommended NuGet Packages

```xml
CommunityToolkit.Mvvm
CommunityToolkit.WinUI
Microsoft.Extensions.DependencyInjection
Microsoft.Data.Sqlite
Serilog
Serilog.Sinks.File
Google.Apis.Drive.v3
Google.Apis.Auth
System.Text.Json
```

## Suggested Project Structure

```text
TextFlowPro
│
├── App
│
├── Views
│   ├── MainWindow
│   ├── SettingsView
│   ├── FindReplaceDialog
│   ├── RecoveryDialog
│   └── GoogleDriveLoginView
│
├── ViewModels
│
├── Models
│   ├── DocumentModel
│   ├── SessionModel
│   ├── SettingsModel
│   └── SyncJobModel
│
├── Services
│   ├── FileService
│   ├── EditorService
│   ├── AutoSaveService
│   ├── RecoveryService
│   ├── SessionService
│   ├── GoogleDriveService
│   ├── SyncQueueService
│   └── SettingsService
│
├── Infrastructure
│
├── Persistence
│
├── Resources
│
└── Tests
```

## Advanced Features

Implement as optional but extensible modules:

### Markdown Preview

Split screen mode:

```text
Editor | Live Preview
```

### Syntax Highlighting

Provide syntax coloring for:

- JSON
- SQL
- Markdown
- CSV

### Recent Files

Show recently opened files on startup.

### Global Search

Search across all opened tabs.

### Document Compare

Side-by-side comparison of two files.

### Future AI Extension Architecture

Design application architecture to support future AI plugins:

- Summarize text
- Rewrite content
- Explain SQL
- Generate Markdown
- Smart search and replace
- Content suggestions

AI modules should use interface-based plugin architecture and dependency injection for future expansion.

## Deliverables

Generate a complete, production-ready WinUI 3 .NET 8 application with:

1. Complete MVVM implementation
2. Multi-tab document editor
3. Google Drive integration and synchronization
4. Auto-save and recovery mechanism
5. Session persistence engine
6. Find and Replace functionality
7. Settings management
8. SQLite persistence
9. Logging infrastructure using Serilog
10. Fluent Design Windows 11 UI
11. Dependency Injection setup
12. Unit test project
13. MSIX packaging configuration
14. Fully documented source code
15. Async/await implementation throughout the solution
16. Clean architecture and SOLID principles

The generated code should be scalable, maintainable, modular, testable, and suitable for long-term production use.