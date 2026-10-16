# Contributing to TextFlow Pro

Thank you for your interest in contributing to **TextFlow Pro**!

## Getting Started

1. **Prerequisites**:
   - Windows 10 (version 1809 or higher) or Windows 11
   - .NET 9.0 SDK
   - Visual Studio 2022 (v17.12+) or VS Code with C# Dev Kit
   - Windows App SDK workloads

2. **Clone the repository**:
   ```bash
   git clone https://github.com/your-username/TextFlow.git
   cd TextFlow
   ```

3. **Build and Test**:
   ```powershell
   dotnet build TextFlow.sln
   dotnet test TextFlow.sln
   ```

## Development Guidelines

- **Architecture**: TextFlow Pro follows Clean Architecture with strict separation between `TextFlow.Core` (platform-agnostic domain, models, services, ViewModels) and `TextFlow` (WinUI 3 presentation layer).
- **MVVM Pattern**: ViewModels use CommunityToolkit.Mvvm source generators (`[ObservableProperty]`, `[RelayCommand]`).
- **Tests**: All business logic, persistence, and ViewModel changes must be backed by unit tests in `tests/TextFlow.Tests/`.
- **Code Style**: Standard C# 12 / 13 conventions, nullable reference types enabled, file-scoped namespaces.

## Pull Request Process

1. Create a feature branch (`git checkout -b feature/my-feature`).
2. Commit your changes with descriptive commit messages.
3. Ensure all tests pass (`dotnet test`).
4. Submit a Pull Request targeting the `main` branch.
