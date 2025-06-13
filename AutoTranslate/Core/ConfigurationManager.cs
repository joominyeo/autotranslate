using System.IO;
using System.Windows.Input;
using Newtonsoft.Json;

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
                var json = JsonConvert.SerializeObject(configuration, Formatting.Indented);
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save configuration: {ex.Message}", ex);
            }
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
                OverlayOpacity = 0.9
            };
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
    }
}