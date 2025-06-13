# AutoTranslate

A Windows desktop application for real-time text translation with global hotkeys.

## Features

- **Global Hotkeys**: Translate selected text from any application using customizable keyboard shortcuts
- **System Tray Integration**: Runs minimized in the background for quick access
- **Multiple Translation Services**: Supports Google Translate and LibreTranslate APIs
- **Overlay Display**: Shows translations in an elegant overlay window near your cursor
- **Configurable Settings**: Customize languages, hotkeys, and behavior
- **Text Capture**: Automatically captures selected text from any Windows application

## How to Use

1. **Installation**: Build and run the application
2. **Configuration**: Set your preferred source and target languages
3. **Hotkey Setup**: Configure your translation hotkey (default: Ctrl + Shift + T)
4. **Translation**: 
   - Select any text in any application
   - Press your configured hotkey
   - View the translation in the overlay window

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

## Usage Notes

- The application requires administrator privileges for global hotkey registration
- Translation services have usage limits and may require API keys for heavy usage
- The overlay window auto-closes after 8 seconds or can be manually closed
- Settings are automatically saved to the user's AppData folder

## Translation Services

1. **Google Translate**: Primary service using public API endpoints
2. **LibreTranslate**: Fallback service for reliability

Both services have usage limitations. For production use, consider obtaining proper API keys.