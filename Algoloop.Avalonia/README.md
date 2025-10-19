# Algoloop Avalonia UI

This is the cross-platform Avalonia UI implementation for Algoloop.

## Quick Start

### Prerequisites
- .NET 8.0 SDK or later

### Build
```bash
dotnet build
```

### Run
```bash
dotnet run
```

## Project Structure

- **Program.cs** - Application entry point
- **App.axaml** / **App.axaml.cs** - Application configuration with FluentTheme
- **MainWindow.axaml** / **MainWindow.axaml.cs** - Main application window
- **ViewModels/** - MVVM view models
- **Styles.axaml** - Application-wide styles
- **Resources/** - Application resources (icons, etc.)

## Features

### Current Implementation
- ✅ Cross-platform desktop application (Windows, Linux, macOS)
- ✅ FluentTheme UI
- ✅ Main window with menu and tabs
- ✅ DataGrid in Log tab
- ✅ MVVM architecture using CommunityToolkit.Mvvm

### Planned Features
- ⏳ Markets management
- ⏳ Strategies configuration
- ⏳ Research/Jupyter integration
- ⏳ Chart visualizations
- ⏳ QuantConnect Lean engine integration
- ⏳ Data persistence
- ⏳ Backtesting capabilities

## Development

### Adding New Views
1. Create `.axaml` and `.axaml.cs` files in the appropriate folder
2. Create corresponding ViewModel in `ViewModels/`
3. Wire up DataContext in code-behind or XAML

### Styling
Application-wide styles are defined in `Styles.axaml`. The project uses the FluentTheme from Avalonia.

### Debugging
The project includes Avalonia.Diagnostics for development builds. Press F12 in the running application to open the DevTools.

## Documentation

For comprehensive migration information, see [AVALONIA_MIGRATION.md](../AVALONIA_MIGRATION.md) in the repository root.

## License

Same license as the main Algoloop project.
