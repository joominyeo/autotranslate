using System.Windows;
using System.Windows.Controls;
using System.Linq;
using AutoTranslate.Core;
using AutoTranslate.Services;
using System.Security;

namespace AutoTranslate.Windows
{
    public partial class SettingsWindow : Window
    {
        private readonly ConfigurationManager _configManager;
        private AppConfiguration _configuration;
        private bool _isLoading = true;

        public SettingsWindow()
        {
            InitializeComponent();
            _configManager = new ConfigurationManager();
            
            InitializeLanguages();
            LoadSettings();
            _isLoading = false;
        }

        private void InitializeLanguages()
        {
            var allLanguages = LanguageManager.GetAllLanguages().ToList();
            var translatableLanguages = LanguageManager.GetTranslatableLanguages().ToList();

            SourceLanguageCombo.ItemsSource = allLanguages;
            SourceLanguageCombo.DisplayMemberPath = "DisplayName";
            SourceLanguageCombo.SelectedValuePath = "Code";

            TargetLanguageCombo.ItemsSource = translatableLanguages;
            TargetLanguageCombo.DisplayMemberPath = "DisplayName";
            TargetLanguageCombo.SelectedValuePath = "Code";
        }

        private void LoadSettings()
        {
            try
            {
                _configuration = _configManager.LoadConfiguration();

                // Language settings
                SourceLanguageCombo.SelectedValue = _configuration.SourceLanguage;
                TargetLanguageCombo.SelectedValue = _configuration.TargetLanguage;
                EnableLanguageDetectionCheckBox.IsChecked = _configuration.EnableLanguageDetection;

                // API settings
                UseOfficialGoogleApiCheckBox.IsChecked = _configuration.UseOfficialGoogleApi;
                ApiKeyPasswordBox.Password = _configuration.GoogleTranslateApiKey;

                // Hotkey settings
                HotkeyTextBox.Text = _configuration.HotkeyDisplayText;

                // Behavior settings
                StartMinimizedCheckBox.IsChecked = _configuration.StartMinimized;
                AutoRestoreClipboardCheckBox.IsChecked = _configuration.AutoRestoreClipboard;
                MaxTextLengthSlider.Value = _configuration.MaxTextLength;

                // Overlay settings
                OverlayDurationSlider.Value = _configuration.OverlayDurationSeconds;
                OverlayOpacitySlider.Value = _configuration.OverlayOpacity;

                UpdateApiKeyPanelState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load settings: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UseOfficialApiCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            UpdateApiKeyPanelState();
        }

        private void UseOfficialApiCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateApiKeyPanelState();
        }

        private void UpdateApiKeyPanelState()
        {
            if (ApiKeyPanel != null)
            {
                ApiKeyPanel.IsEnabled = UseOfficialGoogleApiCheckBox.IsChecked == true;
            }
        }

        private async void TestApiKey_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ApiKeyPasswordBox.Password))
            {
                ApiTestResultTextBlock.Text = "Please enter an API key first.";
                ApiTestResultTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            TestApiKeyButton.IsEnabled = false;
            TestApiKeyButton.Content = "Testing...";
            ApiTestResultTextBlock.Text = "Testing API key...";
            ApiTestResultTextBlock.Foreground = System.Windows.Media.Brushes.Blue;

            try
            {
                using var translationService = new TranslationService();
                
                // Create a temporary config with the API key
                var tempConfig = new AppConfiguration
                {
                    GoogleTranslateApiKey = ApiKeyPasswordBox.Password,
                    UseOfficialGoogleApi = true
                };

                // Test translation
                var result = await translationService.TranslateAsync("Hello", "en", "es");
                
                if (result.Success)
                {
                    ApiTestResultTextBlock.Text = $"✓ API key is valid! Test translation: {result.TranslatedText}";
                    ApiTestResultTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                }
                else
                {
                    ApiTestResultTextBlock.Text = $"✗ API test failed: {result.ErrorMessage}";
                    ApiTestResultTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                ApiTestResultTextBlock.Text = $"✗ API test error: {ex.Message}";
                ApiTestResultTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            }
            finally
            {
                TestApiKeyButton.IsEnabled = true;
                TestApiKeyButton.Content = "Test API Key";
            }
        }

        private void SetHotkey_Click(object sender, RoutedEventArgs e)
        {
            var hotkeyDialog = new HotkeyDialog();
            if (hotkeyDialog.ShowDialog() == true)
            {
                HotkeyTextBox.Text = hotkeyDialog.HotkeyText;
                _configuration.HotkeyModifiers = hotkeyDialog.Modifiers;
                _configuration.HotkeyKey = hotkeyDialog.Key;
                _configuration.HotkeyDisplayText = hotkeyDialog.HotkeyText;
            }
        }

        private void MaxTextLengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoading && MaxTextLengthLabel != null)
            {
                MaxTextLengthLabel.Text = ((int)e.NewValue).ToString();
            }
        }

        private void OverlayDurationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoading && OverlayDurationLabel != null)
            {
                OverlayDurationLabel.Text = $"{(int)e.NewValue}s";
            }
        }

        private void OverlayOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoading && OverlayOpacityLabel != null)
            {
                OverlayOpacityLabel.Text = $"{(int)(e.NewValue * 100)}%";
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to reset all settings to defaults?", 
                "Reset Settings", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
            if (result == MessageBoxResult.Yes)
            {
                _isLoading = true;
                
                // Load default configuration
                var defaultConfig = new AppConfiguration();
                
                // Apply defaults to UI
                SourceLanguageCombo.SelectedValue = defaultConfig.SourceLanguage;
                TargetLanguageCombo.SelectedValue = defaultConfig.TargetLanguage;
                EnableLanguageDetectionCheckBox.IsChecked = defaultConfig.EnableLanguageDetection;
                
                UseOfficialGoogleApiCheckBox.IsChecked = defaultConfig.UseOfficialGoogleApi;
                ApiKeyPasswordBox.Password = defaultConfig.GoogleTranslateApiKey;
                
                HotkeyTextBox.Text = defaultConfig.HotkeyDisplayText;
                
                StartMinimizedCheckBox.IsChecked = defaultConfig.StartMinimized;
                AutoRestoreClipboardCheckBox.IsChecked = defaultConfig.AutoRestoreClipboard;
                MaxTextLengthSlider.Value = defaultConfig.MaxTextLength;
                
                OverlayDurationSlider.Value = defaultConfig.OverlayDurationSeconds;
                OverlayOpacitySlider.Value = defaultConfig.OverlayOpacity;
                
                _configuration = defaultConfig;
                
                UpdateApiKeyPanelState();
                ApiTestResultTextBlock.Text = "";
                
                _isLoading = false;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Update configuration from UI
                _configuration.SourceLanguage = SourceLanguageCombo.SelectedValue?.ToString() ?? "auto";
                _configuration.TargetLanguage = TargetLanguageCombo.SelectedValue?.ToString() ?? "en";
                _configuration.EnableLanguageDetection = EnableLanguageDetectionCheckBox.IsChecked == true;
                
                _configuration.UseOfficialGoogleApi = UseOfficialGoogleApiCheckBox.IsChecked == true;
                _configuration.GoogleTranslateApiKey = ApiKeyPasswordBox.Password;
                
                _configuration.StartMinimized = StartMinimizedCheckBox.IsChecked == true;
                _configuration.AutoRestoreClipboard = AutoRestoreClipboardCheckBox.IsChecked == true;
                _configuration.MaxTextLength = (int)MaxTextLengthSlider.Value;
                
                _configuration.OverlayDurationSeconds = (int)OverlayDurationSlider.Value;
                _configuration.OverlayOpacity = OverlayOpacitySlider.Value;

                // Save configuration
                _configManager.SaveConfiguration(_configuration);
                
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}