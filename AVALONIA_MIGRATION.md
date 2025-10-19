# Avalonia UI Migration Guide

## Overview

This document describes the migration of Algoloop from WPF to Avalonia UI for cross-platform support.

## What Changed

### New Project Structure

- **Algoloop.Avalonia** - New cross-platform UI project using Avalonia UI 11.2.2
- **WPF Projects** - Existing WPF projects remain for Windows compatibility but are not required for the Avalonia version

### Technology Stack

- **UI Framework**: Avalonia UI 11.2.2 (replacing WPF)
- **Target Framework**: .NET 8.0 (cross-platform)
- **MVVM Toolkit**: CommunityToolkit.Mvvm (shared with WPF version)
- **Charts**: OxyPlot.Avalonia 2.1.0 (replacing OxyPlot.Wpf)

## Key Differences from WPF

### 1. File Extensions
- `.xaml` → `.axaml` (Avalonia XAML)
- Application entry point uses `Program.cs` instead of WPF's App.xaml.cs model

### 2. Namespace Changes
- WPF: `xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"`
- Avalonia: `xmlns="https://github.com/avaloniaui"`

### 3. Control Replacements
- **ThemedWindow** (DevExpress) → Standard Avalonia `Window`
- **WebView2** → Pending cross-platform alternative
- **WPF-specific controls** → Avalonia equivalents or custom implementations

### 4. Event Handling
- WPF's Dispatcher → Avalonia's Dispatcher (similar but different API)
- Some WPF-specific events require alternative approaches in Avalonia

## Current Implementation Status

### ✅ Completed
- Basic Avalonia project structure created
- Application entry point (Program.cs, App.axaml)
- Main window with menu and tab layout
- Cross-platform URL opening for help links
- Resource files and icons migrated
- Solution file updated to include Avalonia project

### 🚧 Partial/Placeholder
- **Views**: Placeholder views created for Markets, Strategies, and Logs
- **Research Tab**: WebView2 replacement pending (needs cross-platform browser component)
- **ViewModels**: Simplified locator without full ViewModel integration
- **Themes**: Basic Fluent theme applied, DevExpress theme support pending
- **Charts**: OxyPlot.Avalonia included but not yet integrated

### ❌ Not Yet Implemented
- Full ViewModel integration (requires cross-platform dependencies)
- Data grids and complex controls
- Settings dialog implementation
- About dialog implementation
- Custom converters and behaviors
- Window state persistence
- Full theming system
- StockSharp charting components

## Building and Running

### Prerequisites
- .NET 8.0 SDK or later
- Supported on Windows, Linux, and macOS

### Build Commands

```bash
# Restore packages
dotnet restore Algoloop.Avalonia/Algoloop.Avalonia.csproj

# Build the project
dotnet build Algoloop.Avalonia/Algoloop.Avalonia.csproj

# Run the application
dotnet run --project Algoloop.Avalonia/Algoloop.Avalonia.csproj
```

### Platform-Specific Notes

**Windows**
- Should work out of the box
- Can still use WPF version if needed

**Linux**
- Requires X11 or Wayland display server
- May need additional dependencies (check Avalonia docs)

**macOS**
- Requires macOS 10.13 or later
- May need Xcode command line tools

## Known Issues and Limitations

1. **WebView2 Integration**: The Research tab currently shows a placeholder. A cross-platform browser component is needed.

2. **ViewModel Dependencies**: Full ViewModel integration is pending until all dependencies are made cross-platform compatible.

3. **Advanced Charting**: StockSharp charting components are WPF-specific and need Avalonia alternatives.

4. **DevExpress Controls**: The WPF version uses DevExpress themed controls. Avalonia uses standard controls with Fluent theme.

5. **Window State**: Window position/size persistence not yet implemented for Avalonia.

## Migration Strategy

The migration follows a **coexistence strategy**:

1. **WPF version** remains functional for Windows users
2. **Avalonia version** provides cross-platform support
3. **Shared code** (ViewModels, business logic, services) is gradually refactored to be platform-agnostic
4. **UI-specific code** remains separate in each project

## Next Steps

### High Priority
1. Implement full ViewModel integration with platform-agnostic dependencies
2. Migrate Markets, Strategies, and Logs views with DataGrid controls
3. Find/implement cross-platform browser component for Research tab
4. Implement Settings and About dialogs

### Medium Priority
1. Migrate all converters and value converters to Avalonia
2. Implement window state persistence
3. Add comprehensive theme support
4. Migrate chart views with OxyPlot.Avalonia

### Low Priority
1. Find alternative for StockSharp charting
2. Performance optimization
3. Platform-specific enhancements
4. Accessibility improvements

## Contributing

When contributing to the Avalonia migration:

1. Keep ViewModels and business logic platform-agnostic
2. Use Avalonia best practices for UI code
3. Test on multiple platforms if possible
4. Update this migration guide as you make progress

## Resources

- [Avalonia Documentation](https://docs.avaloniaui.net/)
- [Avalonia Samples](https://github.com/AvaloniaUI/Avalonia.Samples)
- [WPF to Avalonia Migration Guide](https://docs.avaloniaui.net/docs/getting-started/wpf)
- [Original Algoloop Documentation](https://github.com/Capnode/Algoloop/wiki/Documentation)

## License

The Avalonia UI implementation follows the same Apache 2.0 license as the original Algoloop project.

Copyright 2018 Capnode AB
