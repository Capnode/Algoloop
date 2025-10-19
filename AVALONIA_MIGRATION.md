# Avalonia UI Migration Documentation

## Overview

This document describes the WPF to Avalonia UI migration for the Algoloop application. The migration creates a cross-platform UI using Avalonia UI 11.2.2 targeting .NET 8.0.

## Project Structure

### New Project: Algoloop.Avalonia

Located in: `Algoloop.Avalonia/`

**Key Files:**
- `Program.cs` - Application entry point
- `App.axaml` / `App.axaml.cs` - Application configuration with Fluent theme
- `Views/MainWindow.axaml` / `Views/MainWindow.axaml.cs` - Main application window
- `ViewModels/` - Stub ViewModels for initial implementation
- `Model/` - Business logic models
- `Resources/` - UI resources (icons, images)

## How to Run

### Prerequisites
- .NET 8.0 SDK or later
- For Windows: No additional requirements
- For Linux: X11 display server
- For macOS: XQuartz (X11 for macOS)

### Build and Run

```bash
# Navigate to repository root
cd /path/to/Algoloop

# Restore dependencies
dotnet restore Algoloop.Avalonia/Algoloop.Avalonia.csproj

# Build the project
dotnet build Algoloop.Avalonia/Algoloop.Avalonia.csproj

# Run the application
dotnet run --project Algoloop.Avalonia/Algoloop.Avalonia.csproj
```

### Running from Visual Studio
1. Open `Algoloop.sln` in Visual Studio 2022 or later
2. Set `Algoloop.Avalonia` as the startup project
3. Press F5 to run

## Current Implementation Status

### ✅ Completed
- [x] Created Algoloop.Avalonia project targeting .NET 8.0
- [x] Added Avalonia NuGet packages (11.2.2)
- [x] Implemented App.axaml with Fluent theme
- [x] Created MainWindow with basic UI layout
- [x] Added Program.cs entry point
- [x] Integrated ViewModelLocator and dependency injection
- [x] Configured logging infrastructure
- [x] Added resources (icons, images)
- [x] Application builds successfully
- [x] Application starts (requires display server)

### ⚠️ Known Limitations

1. **ViewModels are Stubs**: Current ViewModels are minimal implementations. Full business logic from WPF ViewModels needs to be migrated.

2. **View Implementations**: Tab content views (Markets, Strategies, Research, Logs) show placeholder text. These need to be implemented with proper Avalonia controls.

3. **WPF-Specific Dependencies**: The following WPF-specific features need Avalonia equivalents:
   - DevExpress ThemedWindow → Replaced with standard Avalonia Window
   - WebView2 (for Research tab) → Needs cross-platform alternative
   - Extended WPF Toolkit controls → Need Avalonia replacements
   - OxyPlot.Wpf → Should use OxyPlot.Avalonia
   - StockSharp charting → Needs evaluation for Avalonia compatibility

4. **Missing Features**:
   - Settings dialog
   - About dialog
   - Theme switching
   - Detailed tab implementations
   - Data binding to real business logic
   - Chart implementations

5. **Platform-Specific Considerations**:
   - Window placement/state persistence uses WPF APIs - needs Avalonia solution
   - Some keyboard shortcuts may behave differently
   - File dialogs use platform-native implementations

## Migration Notes

### Replaced Components

| WPF Component | Avalonia Replacement | Status |
|---------------|---------------------|--------|
| dx:ThemedWindow | Window | ✅ Complete |
| System.Windows.Controls.Menu | Avalonia.Controls.Menu | ✅ Complete |
| System.Windows.Controls.TabControl | Avalonia.Controls.TabControl | ✅ Complete |
| StatusBar | Border + TextBlock | ✅ Complete |
| WebView2 | Placeholder TextBlock | ⚠️ Needs replacement |

### Code Changes Made

1. **Removed WPF Dependencies**: 
   - Changed from `UseWPF` to Avalonia SDK
   - Removed Windows-specific targeting
   - Updated to .NET 8.0

2. **XAML Migration**:
   - Changed namespace from `http://schemas.microsoft.com/winfx/2006/xaml/presentation` to `https://github.com/avaloniaui`
   - Updated property names (e.g., `DockPanel.Dock` works the same)
   - Disabled compiled bindings for dynamic resource access

3. **Code-Behind Changes**:
   - Changed `System.Windows.Window` to `Avalonia.Controls.Window`
   - Updated event handler signatures
   - Cross-platform URL opening logic

## Next Steps

To complete the migration:

1. **Implement Full ViewModels**:
   - Port business logic from WPF ViewModels
   - Remove WPF-specific dependencies (Window, DataGridColumn, etc.)
   - Implement proper commands and data binding

2. **Implement View Content**:
   - Create MarketsView.axaml
   - Create StrategiesView.axaml  
   - Create LogView.axaml
   - Replace WebView2 in Research tab

3. **Add Dialogs**:
   - Settings dialog
   - About dialog
   - Other modal dialogs

4. **Charting**:
   - Integrate OxyPlot.Avalonia
   - Port chart implementations

5. **Testing**:
   - Test on Windows, Linux, and macOS
   - Verify data binding and commands
   - Performance testing

## References

- [Avalonia UI Documentation](https://docs.avaloniaui.net/)
- [Avalonia Samples](https://github.com/AvaloniaUI/Avalonia.Samples)
- [WPF to Avalonia Migration Guide](https://docs.avaloniaui.net/docs/guides/platforms/wpf-migration)
- [Avalonia Community](https://github.com/AvaloniaUI/Avalonia/discussions)

## Support

For issues related to the Avalonia migration, please:
1. Check existing GitHub issues
2. Review Avalonia documentation
3. Create a new issue with details about the problem

---

**Last Updated**: October 2025  
**Avalonia Version**: 11.2.2  
**Target Framework**: .NET 8.0
