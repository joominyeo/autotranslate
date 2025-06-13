using System.IO;
using System.Windows.Input;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace AutoTranslate.Core
{
    public class ConfigurationManager
    {
        private readonly string _configPath;
        private readonly string _configDirectory;

        public ConfigurationManager()
        {
            _configDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoTranslate");
            _configPath = Path.Combine(_configDirectory, "config.json");
            
            EnsureConfigDirectoryExists();
        }

        private void EnsureConfigDirectoryExists()
        {
            if (!Directory.Exists(_configDirectory))
            {
                Directory.CreateDirectory(_configDirectory);
            }
        }

        public AppConfiguration LoadConfiguration()
        {
            try
            {
                if (!File.Exists(_configPath))
                {
                    return GetDefaultConfiguration();
                }

                var json = File.ReadAllText(_configPath);
                var config = JsonConvert.DeserializeObject<AppConfiguration>(json);
                
                return config ?? GetDefaultConfiguration();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load configuration: {ex.Message}", ex);
            }
        }

        public void SaveConfiguration(AppConfiguration configuration)
        {
            try
            {
                ValidateConfiguration(configuration);
                var json = JsonConvert.SerializeObject(configuration, Formatting.Indented);
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save configuration: {ex.Message}", ex);
            }
        }

        public void ValidateConfiguration(AppConfiguration config)
        {
            var errors = new List<string>();

            // Validate overlay duration
            if (config.OverlayDurationSeconds < 1 || config.OverlayDurationSeconds > 60)
                errors.Add("Overlay duration must be between 1 and 60 seconds.");

            // Validate overlay opacity
            if (config.OverlayOpacity < 0.1 || config.OverlayOpacity > 1.0)
                errors.Add("Overlay opacity must be between 0.1 and 1.0.");

            // Validate translation timeout
            if (config.TranslationTimeoutSeconds < 5 || config.TranslationTimeoutSeconds > 300)
                errors.Add("Translation timeout must be between 5 and 300 seconds.");

            // Validate max text length
            if (config.MaxTextLength < 10 || config.MaxTextLength > 50000)
                errors.Add("Max text length must be between 10 and 50,000 characters.");

            // Validate font size
            if (config.OverlayFontSize < 8 || config.OverlayFontSize > 72)
                errors.Add("Overlay font size must be between 8 and 72 points.");

            // Validate border thickness
            if (config.OverlayBorderThickness < 0 || config.OverlayBorderThickness > 10)
                errors.Add("Overlay border thickness must be between 0 and 10 pixels.");

            // Validate corner radius
            if (config.OverlayCornerRadius < 0 || config.OverlayCornerRadius > 50)
                errors.Add("Overlay corner radius must be between 0 and 50 pixels.");

            // Validate retry attempts
            if (config.MaxRetryAttempts < 0 || config.MaxRetryAttempts > 10)
                errors.Add("Max retry attempts must be between 0 and 10.");

            // Validate color formats
            if (!IsValidHexColor(config.OverlayBackgroundColor))
                errors.Add("Invalid overlay background color format. Use hex format like #RRGGBB.");

            if (!IsValidHexColor(config.OverlayTextColor))
                errors.Add("Invalid overlay text color format. Use hex format like #RRGGBB.");

            if (!IsValidHexColor(config.OverlayBorderColor))
                errors.Add("Invalid overlay border color format. Use hex format like #RRGGBB.");

            // Validate theme
            if (!new[] { "Auto", "Light", "Dark" }.Contains(config.Theme))
                errors.Add("Theme must be 'Auto', 'Light', or 'Dark'.");

            if (errors.Any())
                throw new ArgumentException($"Configuration validation failed:\n{string.Join("\n", errors)}");
        }

        private static bool IsValidHexColor(string color)
        {
            if (string.IsNullOrWhiteSpace(color))
                return false;

            return System.Text.RegularExpressions.Regex.IsMatch(color, @"^#[0-9A-Fa-f]{6}$");
        }

        private AppConfiguration GetDefaultConfiguration()
        {
            return new AppConfiguration
            {
                SourceLanguage = "auto",
                TargetLanguage = "en",
                HotkeyModifiers = ModifierKeys.Control | ModifierKeys.Shift,
                HotkeyKey = Key.T,
                HotkeyDisplayText = "Ctrl + Shift + T",
                StartMinimized = false,
                TranslationService = "GoogleTranslate",
                OverlayDurationSeconds = 5,
                OverlayOpacity = 0.9,
                GoogleTranslateApiKey = string.Empty,
                UseOfficialGoogleApi = false,
                EnableLanguageDetection = true,
                AutoRestoreClipboard = true,
                MaxTextLength = 5000,
                
                // Overlay appearance defaults
                OverlayBackgroundColor = "#2D2D30",
                OverlayTextColor = "#FFFFFF",
                OverlayBorderColor = "#007ACC",
                OverlayBorderThickness = 2,
                OverlayCornerRadius = 8,
                OverlayFontSize = 14,
                OverlayFontFamily = "Segoe UI",
                OverlayShowDropShadow = true,
                
                // Translation timeout defaults
                TranslationTimeoutSeconds = 10,
                MaxRetryAttempts = 3,
                
                // Startup and notification defaults
                StartWithWindows = false,
                EnableSoundNotifications = false,
                NotificationSoundPath = string.Empty,
                ShowNotificationOnSuccess = true,
                ShowNotificationOnError = true,
                
                // Advanced defaults
                EnableClipboardMonitoring = false,
                ClipboardMonitoringInterval = 1000,
                AutoTranslateOnSelect = false,
                RememberWindowPosition = true,
                MinimizeToTray = true,
                
                // Performance defaults
                EnableTextCaching = true,
                CacheExpireMinutes = 60,
                PreloadCommonLanguages = true,
                
                // UI preferences defaults
                ShowLanguageFlags = true,
                CompactMode = false,
                Theme = "Auto"
            };
        }

        public AppConfiguration GetFreshDefaultConfiguration()
        {
            return GetDefaultConfiguration();
        }
    }

    public class AppConfiguration
    {
        public string SourceLanguage { get; set; } = "auto";
        public string TargetLanguage { get; set; } = "en";
        public ModifierKeys HotkeyModifiers { get; set; } = ModifierKeys.Control | ModifierKeys.Shift;
        public Key HotkeyKey { get; set; } = Key.T;
        public string HotkeyDisplayText { get; set; } = "Ctrl + Shift + T";
        public bool StartMinimized { get; set; } = false;
        public string TranslationService { get; set; } = "GoogleTranslate";
        public int OverlayDurationSeconds { get; set; } = 5;
        public double OverlayOpacity { get; set; } = 0.9;
        public string GoogleTranslateApiKey { get; set; } = string.Empty;
        public bool UseOfficialGoogleApi { get; set; } = false;
        public bool EnableLanguageDetection { get; set; } = true;
        public bool AutoRestoreClipboard { get; set; } = true;
        public int MaxTextLength { get; set; } = 5000;
        
        // New overlay appearance settings
        public string OverlayBackgroundColor { get; set; } = "#2D2D30";
        public string OverlayTextColor { get; set; } = "#FFFFFF";
        public string OverlayBorderColor { get; set; } = "#007ACC";
        public int OverlayBorderThickness { get; set; } = 2;
        public int OverlayCornerRadius { get; set; } = 8;
        public int OverlayFontSize { get; set; } = 14;
        public string OverlayFontFamily { get; set; } = "Segoe UI";
        public bool OverlayShowDropShadow { get; set; } = true;
        
        // Translation timeout settings
        public int TranslationTimeoutSeconds { get; set; } = 10;
        public int MaxRetryAttempts { get; set; } = 3;
        
        // Startup and notification settings
        public bool StartWithWindows { get; set; } = false;
        public bool EnableSoundNotifications { get; set; } = false;
        public string NotificationSoundPath { get; set; } = string.Empty;
        public bool ShowNotificationOnSuccess { get; set; } = true;
        public bool ShowNotificationOnError { get; set; } = true;
        
        // Advanced settings
        public bool EnableClipboardMonitoring { get; set; } = false;
        public int ClipboardMonitoringInterval { get; set; } = 1000;
        public bool AutoTranslateOnSelect { get; set; } = false;
        public bool RememberWindowPosition { get; set; } = true;
        public bool MinimizeToTray { get; set; } = true;
        
        // Performance settings
        public bool EnableTextCaching { get; set; } = true;
        public int CacheExpireMinutes { get; set; } = 60;
        public bool PreloadCommonLanguages { get; set; } = true;
        
        // UI preferences
        public bool ShowLanguageFlags { get; set; } = true;
        public bool CompactMode { get; set; } = false;
        public string Theme { get; set; } = "Auto"; // Auto, Light, Dark
    }
}