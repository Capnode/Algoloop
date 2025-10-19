# Avalonia UI Migration Documentation

## Overview

This document describes the migration from WPF to Avalonia UI for the Algoloop application. The Avalonia UI project provides a cross-platform alternative to the existing WPF application.

## What Changed

### New Project: Algoloop.Avalonia

A new Avalonia UI project has been added to the solution targeting .NET 8.0. This project:
- Uses Avalonia 11.2.2 for cross-platform UI
- Implements a minimal main window with menu and tabbed interface
- Provides standalone ViewModels without WPF dependencies
- Includes the FluentTheme for modern UI styling

### Project Structure

```
Algoloop.Avalonia/
├── Algoloop.Avalonia.csproj  # Project file
├── Program.cs                  # Application entry point
├── App.axaml                   # Application definition
├── App.axaml.cs                # Application code-behind
├── MainWindow.axaml            # Main window XAML
├── MainWindow.axaml.cs         # Main window code-behind
├── Styles.axaml                # Application styles
├── ViewModels/
│   └── MainViewModel.cs        # Main view model
└── Resources/
    └── AlgoloopIcon.ico       # Application icon
```

### Key Components

#### Program.cs
The entry point that configures and starts the Avalonia application with classic desktop lifetime.

#### App.axaml / App.axaml.cs
The main application class that:
- Loads the FluentTheme
- Initializes the application styles
- Creates and shows the main window

#### MainWindow.axaml / MainWindow.axaml.cs
The main application window featuring:
- File menu with Save and Exit commands
- Help menu
- Status bar
- Tabbed interface with:
  - Markets (placeholder)
  - Strategies (placeholder)
  - Research (placeholder)
  - Log (DataGrid with sample data)

#### ViewModels
Simplified ViewModels created specifically for Avalonia:
- **MainViewModel**: Main application view model with Save and Exit commands
- **LogViewModel**: Log view model with sample log entries

### Dependencies

The Avalonia project uses the following NuGet packages:
- Avalonia 11.2.2
- Avalonia.Desktop 11.2.2
- Avalonia.Controls.DataGrid 11.2.2
- Avalonia.Themes.Fluent 11.2.2
- Avalonia.Diagnostics 11.2.2 (Debug only)
- CommunityToolkit.Mvvm 8.4.0

## How to Build and Run

### Prerequisites
- .NET 8.0 SDK or later
- Platform-specific requirements:
  - Windows: No additional requirements
  - Linux: See [Avalonia Linux setup](https://docs.avaloniaui.net/docs/get-started/set-up-an-editor/linux)
  - macOS: See [Avalonia macOS setup](https://docs.avaloniaui.net/docs/get-started/set-up-an-editor/macos)

### Build Instructions

#### Build the Avalonia project:
```bash
dotnet build Algoloop.Avalonia/Algoloop.Avalonia.csproj
```

#### Build the entire solution:
```bash
dotnet build Algoloop.sln
```

### Run Instructions

#### Run the Avalonia UI application:
```bash
dotnet run --project Algoloop.Avalonia
```

#### On Windows, you can also run the executable directly:
```bash
.\Algoloop.Avalonia\bin\Debug\net8.0\Algoloop.Avalonia.exe
```

## Known Limitations

### Current Implementation Status

1. **Minimal UI Implementation**
   - The current implementation provides a basic window structure
   - Only the Log tab has a functional DataGrid with sample data
   - Markets, Strategies, and Research tabs show placeholders

2. **ViewModels**
   - Simplified ViewModels created specifically for Avalonia
   - No integration with existing WPF ViewModels (which have WPF dependencies)
   - Business logic from Algoloop core projects needs to be integrated

3. **Missing Features**
   - No integration with QuantConnect Lean engine
   - No data loading or persistence
   - No charts or visualizations
   - No strategy/market management
   - No settings or configuration UI

4. **Platform-Specific Limitations**
   - The original WPF ViewModels use System.Windows which is Windows-specific
   - Some features may require platform-specific implementations

### WPF Project Status

- The WPF project (Algoloop.Wpf) remains unchanged and fully functional
- Both WPF and Avalonia projects can coexist in the solution
- WPF is still the primary/recommended UI for production use

## Next Steps for Full Migration

To achieve feature parity with the WPF version, the following work is needed:

1. **Refactor ViewModels**
   - Extract business logic from WPF-specific ViewModels
   - Create platform-agnostic ViewModels or shared view model library
   - Implement proper MVVM patterns without WPF dependencies

2. **Implement Core Features**
   - Markets management UI
   - Strategies management UI
   - Research/Jupyter notebook integration
   - Settings and configuration
   - Chart visualizations (using OxyPlot.Avalonia or similar)

3. **Integrate Business Logic**
   - Connect to QuantConnect Lean engine
   - Implement data loading and persistence
   - Add backtesting capabilities
   - Integrate market data providers

4. **Testing and Validation**
   - Cross-platform testing (Windows, Linux, macOS)
   - UI/UX refinement
   - Performance optimization

5. **Documentation**
   - User guide for Avalonia version
   - Developer documentation
   - Migration guide for contributors

## Architecture Notes

### Separation of Concerns

The Avalonia implementation intentionally avoids direct references to WPF projects to maintain cross-platform compatibility. Future development should:
- Keep UI code platform-agnostic where possible
- Use dependency injection for platform-specific services
- Share business logic through non-UI projects

### Recommended Approach

For a production-ready Avalonia migration:
1. Create shared ViewModel library (no WPF/Avalonia dependencies)
2. Use interfaces for platform-specific services
3. Implement proper dependency injection
4. Use platform-specific view implementations (WPF/Avalonia)

## References

- [Avalonia Documentation](https://docs.avaloniaui.net/)
- [Avalonia Samples](https://github.com/AvaloniaUI/Avalonia.Samples)
- [MVVM Toolkit](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [WPF to Avalonia Migration Guide](https://docs.avaloniaui.net/docs/guides/platforms/wpf-migration)

## Support

For questions or issues related to the Avalonia migration:
1. Check existing documentation
2. Review Avalonia samples and documentation
3. Create an issue in the repository with "Avalonia" label

## License

The Avalonia project follows the same license as the main Algoloop project.
