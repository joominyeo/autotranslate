using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using AutoTranslate.Core;
using AutoTranslate.Services;

namespace AutoTranslate.Windows
{
    public partial class AboutWindow : Window
    {
        private readonly UsageStatistics _statistics;
        private readonly ConfigurationManager _configManager;

        public AboutWindow()
        {
            InitializeComponent();
            
            _statistics = new UsageStatistics();
            _configManager = new ConfigurationManager();
            
            LoadInformation();
        }

        private void LoadInformation()
        {
            try
            {
                // Version information
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                VersionTextBlock.Text = $"Version {version?.ToString(3) ?? "1.0.0"}";

                // Build date
                var buildDate = File.GetCreationTime(assembly.Location);
                BuildDateTextBlock.Text = buildDate.ToString("yyyy-MM-dd");

                // System information
                OSVersionTextBlock.Text = Environment.OSVersion.ToString();
                DotNetVersionTextBlock.Text = Environment.Version.ToString();

                // Usage statistics
                var stats = _statistics.GetStatistics();
                TotalTranslationsTextBlock.Text = stats.TotalTranslations.ToString("N0");
                SuccessRateTextBlock.Text = $"{_statistics.GetSuccessRate():F1}%";
                CharactersTranslatedTextBlock.Text = stats.TotalCharactersTranslated.ToString("N0");
                MostUsedLanguagePairTextBlock.Text = _statistics.GetMostUsedLanguagePair();
                
                var usageDuration = _statistics.GetUsageDuration();
                DaysInUseTextBlock.Text = usageDuration.Days > 0 ? $"{usageDuration.Days}" : "< 1";

                // Current hotkey
                var config = _configManager.LoadConfiguration();
                HotkeyTextBlock.Text = config.HotkeyDisplayText;
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading about information", ex);
            }
        }

        private void ConfigFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var configDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "AutoTranslate");
                
                if (Directory.Exists(configDirectory))
                {
                    Process.Start("explorer.exe", configDirectory);
                }
                else
                {
                    MessageBox.Show("Configuration folder not found.", "Folder Not Found", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error opening config folder", ex);
                MessageBox.Show($"Failed to open configuration folder: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LogFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var logDirectory = Logger.GetLogDirectory();
                
                if (Directory.Exists(logDirectory))
                {
                    Process.Start("explorer.exe", logDirectory);
                }
                else
                {
                    MessageBox.Show("Log folder not found.", "Folder Not Found", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error opening log folder", ex);
                MessageBox.Show($"Failed to open log folder: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetStatistics_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "Are you sure you want to reset all usage statistics? This action cannot be undone.",
                    "Reset Statistics",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _statistics.ResetStatistics();
                    LoadInformation(); // Refresh the display
                    
                    MessageBox.Show("Usage statistics have been reset.", "Statistics Reset", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error resetting statistics", ex);
                MessageBox.Show($"Failed to reset statistics: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}