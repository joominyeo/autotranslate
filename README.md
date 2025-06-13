# AutoTranslate

A powerful Windows application for real-time text translation with global hotkeys and customizable overlay display.

## Features

- **Global Hotkey Support**: Translate selected text instantly from any application
- **100+ Languages**: Support for all major languages with automatic detection
- **Customizable Overlay**: Personalize appearance, colors, fonts, and animations
- **Multiple Translation Services**: Google Translate and LibreTranslate with automatic fallback
- **Smart Caching**: Avoid duplicate API calls with intelligent translation caching
- **Usage Statistics**: Track your translation habits and productivity
- **System Tray Integration**: Always available without cluttering your desktop
- **Comprehensive Settings**: Extensive customization options for every preference
- **Export/Import Settings**: Share configurations between devices

## Quick Start

1. **Download and Install** AutoTranslate from the releases page
2. **Launch the application** - it will appear in your system tray
3. **Select any text** anywhere on your screen
4. **Press `Ctrl + Shift + T`** (default hotkey) to translate
5. **View the translation** in the overlay window
6. **Click the overlay** to copy the translation to clipboard

## System Requirements

- Windows 10 or Windows 11
- .NET 8.0 Runtime (automatically installed)
- Internet connection for translation services
- 50 MB free disk space

## Installation

### Automatic Installer (Recommended)
1. Download `AutoTranslate-Setup.exe` from the latest release
2. Run the installer and follow the setup wizard
3. Launch AutoTranslate from the Start Menu or desktop shortcut

### Manual Installation
1. Download `AutoTranslate-Portable.zip`
2. Extract to your preferred folder
3. Run `AutoTranslate.exe`
4. Optionally enable "Start with Windows" in settings

## Configuration

### API Key Setup (Recommended)

For the best translation quality and reliability, configure a Google Translate API key:

1. Visit [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select an existing one
3. Enable the "Cloud Translation API"
4. Create credentials (API Key)
5. In AutoTranslate: **Settings → Language & Translation → Use Official Google Translate API**
6. Enter your API key and click "Test API Key"

> **Note**: Without an API key, AutoTranslate uses free public endpoints which may have limitations.

### Hotkey Customization

1. Open **Settings → Hotkeys & Behavior**
2. Click **"Change"** next to "Translation Hotkey"
3. Press your desired key combination
4. Click **"OK"** to save

### Overlay Appearance

Customize the translation overlay appearance:

1. Open **Settings → Overlay Appearance**
2. Adjust colors, fonts, opacity, and effects
3. Use the **live preview** to see changes in real-time
4. Configure auto-close duration and border styles

## Usage Guide

### Basic Translation
- Select text in any application
- Press your configured hotkey (`Ctrl + Shift + T` by default)
- Translation appears in an overlay window
- Click the overlay to copy translation
- Overlay auto-closes after configured duration

### Advanced Features

#### Translation History
View your recent translations:
- **About → Usage Statistics** for summary
- Translation history is automatically saved
- Search through past translations

#### Caching
- Repeated translations are served from cache instantly
- Cache expires based on your settings
- Improves performance and reduces API usage

#### Startup with Windows
- **Settings → Hotkeys & Behavior → Start with Windows**
- Application starts automatically when Windows boots
- Begins minimized to system tray

#### Sound Notifications
- **Settings → Notifications → Enable sound notifications**
- Choose custom sound files for translation events
- Configure different sounds for success/error states

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl + Shift + T` | Translate selected text (configurable) |
| `F1` | Show help window |
| `Escape` | Close overlay window |
| `Ctrl + C` | Copy translation (when overlay focused) |
| `Alt + F4` | Close application |

## Troubleshooting

### Translation Not Working

**Check Internet Connection**
- Ensure stable internet connectivity
- Try translating simple text first

**Verify Text Selection**
- Make sure text is properly selected before pressing hotkey
- Some applications may not support text selection

**API Key Issues**
- Test your API key in Settings → Language & Translation
- Ensure the API key has Translation API permissions
- Check Google Cloud Console for usage limits

### Hotkey Not Responding

**Hotkey Conflicts**
- Another application might be using the same hotkey
- Change hotkey in Settings → Hotkeys & Behavior
- Try administrator privileges if needed

**Application Status**
- Check if AutoTranslate is running (system tray icon)
- Restart the application if needed

### Overlay Not Appearing

**Display Settings**
- Check if overlay opacity is too low
- Overlay might be off-screen on multi-monitor setups
- Reset overlay settings to defaults

**Software Conflicts**
- Disable other overlay software temporarily
- Check antivirus software for blocking

### Performance Issues

**Enable Caching**
- Settings → Hotkeys & Behavior → Enable translation caching
- Reduces API calls for repeated translations

**Optimize Settings**
- Reduce max text length for large translations
- Disable unnecessary features like clipboard monitoring
- Clear translation cache if it becomes large

## File Locations

### Configuration Files
```
%APPDATA%\AutoTranslate\
├── config.json              # Application settings
├── translation_cache.json   # Translation cache
├── translation_history.json # Translation history
├── usage_statistics.json    # Usage analytics
└── Logs\                    # Application logs
    └── autotranslate_YYYY-MM-DD.log
```

### Accessing Configuration
- **Settings → Advanced → Open Config Folder**
- Manually navigate to `%APPDATA%\AutoTranslate`

## Settings Backup & Restore

### Export Settings
1. **Settings → Advanced → Export Settings**
2. Choose location and filename
3. Share `.ats` file with other devices

### Import Settings
1. **Settings → Advanced → Import Settings**
2. Select `.ats` or `.json` configuration file
3. Confirm to overwrite current settings

## Command Line Options

```bash
AutoTranslate.exe [options]

Options:
  --minimized    Start minimized to system tray
  --config PATH  Use custom configuration file
  --reset        Reset all settings to defaults
  --help         Show command line help
```

## FAQ

**Q: Is AutoTranslate free to use?**
A: Yes, AutoTranslate is completely free and open-source.

**Q: Do I need a Google API key?**
A: No, but it's recommended for better reliability and no rate limits.

**Q: Can I use AutoTranslate offline?**
A: No, internet connection is required for translation services.

**Q: How much data does AutoTranslate use?**
A: Minimal - only the text being translated is sent to servers.

**Q: Is my text data secure?**
A: Text is sent to translation services (Google/LibreTranslate) but not stored by AutoTranslate.

**Q: Can I translate images or PDFs?**
A: Only text content. Extract text from images/PDFs first using OCR tools.

**Q: Does AutoTranslate work with all applications?**
A: Yes, it works with any application that supports text selection.

## Support

### Getting Help
- Press `F1` in the application for help topics
- Check the troubleshooting section above
- Review log files in the configuration folder

### Reporting Issues
1. Collect relevant information:
   - Windows version
   - AutoTranslate version
   - Error messages
   - Log files
2. Create a detailed issue report
3. Include steps to reproduce the problem

### Feature Requests
We welcome suggestions for new features and improvements.

## Privacy Policy

- AutoTranslate only sends text to translation services when you explicitly request translation
- No personal data is collected or transmitted
- Usage statistics are stored locally only
- API keys are stored locally and encrypted
- Translation history is kept local unless you export it

## Technical Details

### Architecture

- **WPF Application**: Built using .NET 8 and Windows Presentation Foundation
- **Windows API Integration**: Uses Win32 APIs for global hotkey registration and text capture
- **Translation Services**: Integrates with multiple translation APIs for reliability
- **Configuration Management**: JSON-based settings storage in user AppData

### Key Components

- `MainWindow`: Primary application interface with system tray support
- `HotkeyManager`: Handles global hotkey registration using Windows API
- `TranslationService`: Manages translation requests to external APIs
- `TextCapture`: Captures selected text using clipboard operations
- `OverlayWindow`: Displays translation results in an overlay
- `ConfigurationManager`: Handles application settings persistence

### Dependencies

- Newtonsoft.Json (13.0.3): JSON serialization
- Hardcodet.NotifyIcon.Wpf (1.1.0): System tray functionality
- Microsoft.Win32.Registry (5.0.0): Windows registry access

### Build Requirements

- .NET 8 SDK
- Windows 10 or later
- Visual Studio 2022 or compatible IDE

## Building and Deployment

### Prerequisites

1. **Install .NET 8 SDK**
   - Download from [Microsoft .NET Downloads](https://dotnet.microsoft.com/download/dotnet/8.0)
   - Verify installation: `dotnet --version`

2. **Install WiX Toolset (for .msi installer)**
   - Download from [WiX Toolset](https://wixtoolset.org/releases/)
   - Or install via Visual Studio extension

### Quick Build

For a simple build to test the application:

```bash
# Navigate to project directory
cd AutoTranslate

# Build the project
dotnet build --configuration Release
```

The executable will be available at:
`AutoTranslate\bin\Release\net8.0-windows\AutoTranslate.exe`

### Production Build

Use the provided build script for a complete production build:

```bash
# Run the build script
build.bat
```

This will:
- Restore NuGet packages
- Build in Release configuration
- Display the output path for the executable

### Creating Distribution Packages

#### Automated Deployment

Use the deployment script to create both portable and installer packages:

```bash
# Run the deployment script
deploy.bat
```

This creates:
- **Portable Version**: `Deploy\AutoTranslate-Portable.zip`
- **Installer Files**: `Deploy\Installer\` (ready for .msi creation)
- **Documentation**: `Deploy\README.md`

#### Manual Deployment Steps

1. **Build Release Version**
   ```bash
   dotnet clean --configuration Release
   dotnet restore
   dotnet build --configuration Release
   ```

2. **Create Portable Package**
   ```bash
   # Copy all files from bin\Release\net8.0-windows\ to a new folder
   # Compress into AutoTranslate-Portable.zip
   ```

3. **Prepare for Installer**
   ```bash
   # Copy executable and dependencies to installer staging folder
   # Include icon.ico and README.md
   ```

### Creating MSI Installer

#### Using WiX Toolset

1. **Create WiX Project File** (`AutoTranslate.wxs`):
   ```xml
   <?xml version="1.0" encoding="UTF-8"?>
   <Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
     <Product Id="*" Name="AutoTranslate" Language="1033" Version="1.0.0.0" 
              Manufacturer="AutoTranslate" UpgradeCode="YOUR-UPGRADE-CODE">
       <Package InstallerVersion="200" Compressed="yes" InstallScope="perMachine" />
       
       <MajorUpgrade DowngradeErrorMessage="A newer version is already installed." />
       <MediaTemplate EmbedCab="yes" />
       
       <Feature Id="ProductFeature" Title="AutoTranslate" Level="1">
         <ComponentGroupRef Id="ProductComponents" />
       </Feature>
     </Product>
     
     <Fragment>
       <Directory Id="TARGETDIR" Name="SourceDir">
         <Directory Id="ProgramFilesFolder">
           <Directory Id="INSTALLFOLDER" Name="AutoTranslate" />
         </Directory>
       </Directory>
     </Fragment>
     
     <Fragment>
       <ComponentGroup Id="ProductComponents" Directory="INSTALLFOLDER">
         <Component Id="MainExecutable">
           <File Source="$(var.SourceDir)\AutoTranslate.exe" />
         </Component>
         <!-- Add other components for DLLs and resources -->
       </ComponentGroup>
     </Fragment>
   </Wix>
   ```

2. **Build MSI**:
   ```bash
   # Compile WiX project
   candle AutoTranslate.wxs -dSourceDir="Deploy\Installer"
   light AutoTranslate.wixobj -o AutoTranslate.msi
   ```

#### Alternative: Using Visual Studio Installer Project

1. Add "Microsoft Visual Studio Installer Projects" extension
2. Create new Setup Project in Visual Studio
3. Add Primary Output from AutoTranslate project
4. Configure installer properties (name, version, etc.)
5. Build to generate .msi file

### Publishing Self-Contained Executable

For a standalone .exe that doesn't require .NET runtime:

```bash
# Publish as self-contained
dotnet publish --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true
```

This creates a single .exe file in:
`AutoTranslate\bin\Release\net8.0-windows\win-x64\publish\AutoTranslate.exe`

### Code Signing (Optional)

For distribution, sign the executable:

```bash
# Using Windows SDK signtool
signtool sign /f "certificate.pfx" /p "password" /t "http://timestamp.comodoca.com" AutoTranslate.exe
```

### Continuous Integration

Example GitHub Actions workflow (`.github/workflows/build.yml`):

```yaml
name: Build and Release

on:
  push:
    tags:
      - 'v*'

jobs:
  build:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
        
    - name: Build
      run: |
        dotnet restore
        dotnet build --configuration Release
        
    - name: Create Packages
      run: deploy.bat
      
    - name: Upload Artifacts
      uses: actions/upload-artifact@v3
      with:
        name: AutoTranslate-Release
        path: Deploy/
```

## License

AutoTranslate is open-source software. See LICENSE file for details.

---

**AutoTranslate** - Making language barriers disappear, one translation at a time.