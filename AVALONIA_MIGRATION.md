# Avalonia UI Migration

This document describes the conversion of the Algoloop WPF UI to Avalonia UI.

## Overview

The Algoloop application has been migrated from Windows Presentation Foundation (WPF) to Avalonia UI, enabling cross-platform support while maintaining the familiar MVVM architecture.

## What Was Changed

### New Project Added
- **Algoloop.UI.Avalonia**: New cross-platform UI project targeting .NET 8.0

### Project Structure
```
Algoloop.UI.Avalonia/
├── Algoloop.UI.Avalonia.csproj  # Project file with Avalonia packages
├── Program.cs                    # Application entry point
├── App.axaml / App.axaml.cs     # Avalonia application definition
├── Styles.axaml                  # Application-wide styles
├── MainWindow.axaml / .cs        # Main application window
├── ViewModels/
│   └── MainViewModel.cs          # Main view model
└── app.manifest                  # Windows application manifest
```

### Dependencies
The Avalonia UI project uses the following NuGet packages:
- **Avalonia** (11.0.10): Core Avalonia framework
- **Avalonia.Desktop** (11.0.10): Desktop platform support
- **Avalonia.Themes.Fluent** (11.0.10): Modern Fluent theme
- **Avalonia.Controls.DataGrid** (11.0.10): DataGrid control
- **Avalonia.Diagnostics** (11.0.10): Development diagnostics (Debug only)
- **CommunityToolkit.Mvvm** (8.4.0): MVVM helpers

### Modified Files
- **Algoloop.sln**: Updated to include the new Avalonia project
- **Algoloop.Wpf.Model/Algoloop.Wpf.Model.csproj**: Added `EnableWindowsTargeting` property to support building on Linux

## How to Build and Run

### Prerequisites
- .NET 8.0 SDK or later
- For development: Visual Studio 2022, Rider, or VS Code with C# extension

### Building the Avalonia UI

```bash
# Navigate to the Avalonia project directory
cd Algoloop.UI.Avalonia

# Restore dependencies and build
dotnet build

# Or build the entire solution
cd ..
dotnet build Algoloop.sln
```

### Running the Avalonia UI

```bash
# From the Avalonia project directory
cd Algoloop.UI.Avalonia
dotnet run
```

Or on Windows, you can run the compiled executable:
```bash
.\bin\Debug\net8.0\Algoloop.UI.Avalonia.exe
```

## Current Implementation Status

### ✅ Completed
- [x] Project structure created with proper Avalonia configuration
- [x] Application entry point (Program.cs) with classic desktop lifetime
- [x] App.axaml with Fluent theme integration
- [x] Styles.axaml for custom styling
- [x] MainWindow with menu and placeholder UI
- [x] Basic MainViewModel with sample commands
- [x] Project successfully builds without errors
- [x] Solution file updated to include new project

### 🚧 Current Limitations and Known Issues

1. **Minimal ViewModel Implementation**: 
   - Current MainViewModel is a minimal placeholder
   - Does not integrate with existing WPF ViewModels from Algoloop.Wpf.ViewModels
   - Real-world functionality would require adapting or sharing ViewModels

2. **UI Controls Placeholder**:
   - Main window has basic menu and tabs
   - No DataGrid implementation yet (package is referenced but not used)
   - Tabs contain simple placeholder text blocks
   - Missing advanced controls like charts (OxyPlot, StockSharp equivalents needed)

3. **Missing Features**:
   - No settings dialog
   - No theme switching implementation
   - No About dialog
   - No integration with backend services
   - No data loading or display

4. **WPF-Specific Controls Not Yet Replaced**:
   - DevExpress ThemedWindow → Standard Avalonia Window (done)
   - Extended WPF Toolkit controls → Need Avalonia equivalents
   - OxyPlot.Wpf charts → Need Avalonia.OxyPlot or alternative
   - StockSharp charting → Need cross-platform charting solution

5. **Dependency Isolation**:
   - Currently references only Algoloop (core library)
   - Does not reference Algoloop.Wpf.Model to avoid Windows-specific dependencies
   - Would need to refactor shared models for cross-platform use

## Next Steps for Full Migration

### Short Term (Essential for MVP)
1. Implement DataGrid with sample data bindings
2. Add navigation between different views (Strategies, Markets, Research, etc.)
3. Integrate with existing core business logic from Algoloop library
4. Add basic error handling and logging display

### Medium Term (Feature Parity)
1. Port or adapt all ViewModels from Algoloop.Wpf.ViewModels
2. Implement all dialogs (Settings, About, etc.)
3. Add charting support using Avalonia-compatible libraries
4. Implement theme switching (Light/Dark modes)
5. Add all menu commands and functionality

### Long Term (Enhancements)
1. Optimize for cross-platform operation (Linux, macOS)
2. Improve UI/UX with Avalonia-specific enhancements
3. Add touch and mobile support if needed
4. Performance optimization for large datasets
5. Comprehensive testing on all platforms

## Architecture Notes

### MVVM Pattern
The application follows the MVVM (Model-View-ViewModel) pattern:
- **Models**: Business logic in Algoloop core library
- **ViewModels**: Located in `ViewModels/` folder, using CommunityToolkit.Mvvm
- **Views**: AXAML files in the root and potential Views folder

### Data Binding
- Uses Avalonia's data binding system
- Compiled bindings disabled by default for flexibility
- Commands use RelayCommand from CommunityToolkit.Mvvm

### Styling
- Uses Fluent theme for modern look and feel
- Custom styles in Styles.axaml
- Theme-aware resource dictionaries

## Building for Different Platforms

### Windows
```bash
dotnet publish -c Release -r win-x64 --self-contained
```

### Linux
```bash
dotnet publish -c Release -r linux-x64 --self-contained
```

### macOS
```bash
dotnet publish -c Release -r osx-x64 --self-contained
```

## Troubleshooting

### Build Issues
- **Error: Project not compatible**: Ensure all referenced projects target compatible frameworks
- **Missing types**: Run `dotnet restore` to restore NuGet packages

### Runtime Issues
- **Window doesn't open**: Check that MainWindow is properly registered in App.axaml.cs
- **Bindings don't work**: Verify DataContext is set and properties are public

## Resources

- [Avalonia Documentation](https://docs.avaloniaui.net/)
- [Avalonia Samples](https://github.com/AvaloniaUI/Avalonia.Samples)
- [Migrating from WPF](https://docs.avaloniaui.net/docs/next/get-started/wpf-migration)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)

## Contributing

When contributing to the Avalonia UI migration:
1. Follow the existing MVVM pattern
2. Use CommunityToolkit.Mvvm for ViewModels
3. Place reusable styles in Styles.axaml
4. Test on multiple platforms when possible
5. Update this document with significant changes
