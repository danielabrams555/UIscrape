# UIScrape - Windows UI Element Inspector

A Windows application that scrapes and displays UI elements from other running applications using the Windows UI Automation API.

## Features

- **Application Selection**: Choose from any visible windowed application currently running
- **Live Auto-Refresh**: Automatically refresh the UI element list at configurable intervals (1-10 seconds)
- **Tree View**: Hierarchical view of UI elements showing parent-child relationships
- **List View**: Flat list view with sortable columns for quick scanning
- **Element Details**: Detailed information panel showing all properties of selected elements
- **Filtering**: Search/filter elements by name, automation ID, control type, or class name
- **Real-time Status**: Visual indicators showing scan status and auto-refresh state

## Requirements

- Windows 10/11
- .NET 8.0 Runtime (or use the self-contained build)

## Building

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (optional)

### Build from Command Line

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run
dotnet run --project src/UIScrape/UIScrape.csproj
```

### Build Release

```bash
# Build Release configuration
dotnet build -c Release

# Publish self-contained executable
dotnet publish src/UIScrape/UIScrape.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## GitHub Actions

This project includes automatic CI/CD through GitHub Actions:

- **On every push**: Builds both Debug and Release configurations
- **On push to main/master**: Creates published artifacts (self-contained and framework-dependent)
- Artifacts are available for download from the Actions tab

## Usage

1. Launch UIScrape
2. Select a target application from the dropdown list
3. Click "Scan Now" to discover UI elements
4. Enable "Auto-Refresh" for live updates
5. Toggle between Tree View and List View as needed
6. Use the filter box to search for specific elements
7. Click on any element to view its detailed properties

## UI Element Properties

For each discovered element, the following properties are displayed:

- **Control Type**: The type of UI control (Button, TextBox, etc.)
- **Name**: The accessible name of the element
- **Automation ID**: The unique automation identifier
- **Class Name**: The underlying window class name
- **Is Enabled**: Whether the element is enabled
- **Is Offscreen**: Whether the element is currently visible
- **Bounding Rectangle**: The screen coordinates and size
- **Value**: The current value (for input controls)

## Architecture

```
UIScrape/
├── src/UIScrape/
│   ├── Converters/       # WPF value converters
│   ├── Models/           # Data models
│   ├── Services/         # UI Automation service
│   ├── ViewModels/       # MVVM ViewModels
│   ├── App.xaml          # Application entry point
│   └── MainWindow.xaml   # Main window UI
├── .github/workflows/    # GitHub Actions CI/CD
└── UIScrape.sln          # Solution file
```

## License

MIT License
