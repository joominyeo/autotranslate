using System.Windows;
using System.Windows.Controls;
using System.Linq;
using AutoTranslate.Core;
using AutoTranslate.Services;
using System.Security;
using System.Windows.Media;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Reflection;
using System.Windows.Media.Effects;

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
            InitializeFontFamilies();
            InitializeUI();
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

        private void InitializeFontFamilies()
        {
            var fonts = System.Windows.Media.Fonts.SystemFontFamilies
                .Select(f => f.Source)
                .OrderBy(f => f)
                .ToList();
            
            FontFamilyCombo.ItemsSource = fonts;
        }

        private void InitializeUI()
        {
            // Set theme combo selection
            ThemeCombo.SelectedIndex = 0; // Auto

            // Set debug information
            VersionTextBlock.Text = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
            BuildDateTextBlock.Text = File.GetCreationTime(Assembly.GetExecutingAssembly().Location).ToString("yyyy-MM-dd");
            OSVersionTextBlock.Text = Environment.OSVersion.ToString();

            // Set config location
            ConfigLocationTextBox.Text = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                "AutoTranslate", 
                "config.json");
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

                // Translation timeout settings
                TranslationTimeoutSlider.Value = _configuration.TranslationTimeoutSeconds;
                MaxRetryAttemptsSlider.Value = _configuration.MaxRetryAttempts;

                // Hotkey settings
                HotkeyTextBox.Text = _configuration.HotkeyDisplayText;

                // Behavior settings
                StartMinimizedCheckBox.IsChecked = _configuration.StartMinimized;
                StartWithWindowsCheckBox.IsChecked = _configuration.StartWithWindows;
                MinimizeToTrayCheckBox.IsChecked = _configuration.MinimizeToTray;
                AutoRestoreClipboardCheckBox.IsChecked = _configuration.AutoRestoreClipboard;
                RememberWindowPositionCheckBox.IsChecked = _configuration.RememberWindowPosition;
                MaxTextLengthSlider.Value = _configuration.MaxTextLength;

                // Advanced features
                EnableClipboardMonitoringCheckBox.IsChecked = _configuration.EnableClipboardMonitoring;
                AutoTranslateOnSelectCheckBox.IsChecked = _configuration.AutoTranslateOnSelect;

                // Performance settings
                EnableTextCachingCheckBox.IsChecked = _configuration.EnableTextCaching;
                PreloadCommonLanguagesCheckBox.IsChecked = _configuration.PreloadCommonLanguages;
                CacheExpireSlider.Value = _configuration.CacheExpireMinutes;

                // Overlay display settings
                OverlayDurationSlider.Value = _configuration.OverlayDurationSeconds;
                OverlayOpacitySlider.Value = _configuration.OverlayOpacity;

                // Color settings
                BackgroundColorTextBox.Text = _configuration.OverlayBackgroundColor;
                TextColorTextBox.Text = _configuration.OverlayTextColor;
                BorderColorTextBox.Text = _configuration.OverlayBorderColor;
                UpdateColorPreviews();

                // Typography settings
                FontFamilyCombo.SelectedItem = _configuration.OverlayFontFamily;
                FontSizeSlider.Value = _configuration.OverlayFontSize;

                // Border settings
                BorderThicknessSlider.Value = _configuration.OverlayBorderThickness;
                CornerRadiusSlider.Value = _configuration.OverlayCornerRadius;
                ShowDropShadowCheckBox.IsChecked = _configuration.OverlayShowDropShadow;

                // Notification settings
                EnableSoundNotificationsCheckBox.IsChecked = _configuration.EnableSoundNotifications;
                SoundFileTextBox.Text = _configuration.NotificationSoundPath;
                ShowNotificationOnSuccessCheckBox.IsChecked = _configuration.ShowNotificationOnSuccess;
                ShowNotificationOnErrorCheckBox.IsChecked = _configuration.ShowNotificationOnError;

                // UI preferences
                ShowLanguageFlagsCheckBox.IsChecked = _configuration.ShowLanguageFlags;
                CompactModeCheckBox.IsChecked = _configuration.CompactMode;
                
                // Set theme selection
                var themeItem = ThemeCombo.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(item => item.Tag.ToString() == _configuration.Theme);
                if (themeItem != null)
                    ThemeCombo.SelectedItem = themeItem;

                UpdateApiKeyPanelState();
                UpdateSoundSettingsPanelState();
                UpdateClipboardMonitoringState();
                UpdateOverlayPreview();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load settings: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Event Handlers - API Settings

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
                ApiTestResultTextBlock.Foreground = Brushes.Red;
                return;
            }

            TestApiKeyButton.IsEnabled = false;
            TestApiKeyButton.Content = "Testing...";
            ApiTestResultTextBlock.Text = "Testing API key...";
            ApiTestResultTextBlock.Foreground = Brushes.Blue;

            try
            {
                using var translationService = new TranslationService();
                
                var tempConfig = new AppConfiguration
                {
                    GoogleTranslateApiKey = ApiKeyPasswordBox.Password,
                    UseOfficialGoogleApi = true
                };

                var result = await translationService.TranslateAsync("Hello", "en", "es");
                
                if (result.Success)
                {
                    ApiTestResultTextBlock.Text = $"✓ API key is valid! Test translation: {result.TranslatedText}";
                    ApiTestResultTextBlock.Foreground = Brushes.Green;
                }
                else
                {
                    ApiTestResultTextBlock.Text = $"✗ API test failed: {result.ErrorMessage}";
                    ApiTestResultTextBlock.Foreground = Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                ApiTestResultTextBlock.Text = $"✗ API test error: {ex.Message}";
                ApiTestResultTextBlock.Foreground = Brushes.Red;
            }
            finally
            {
                TestApiKeyButton.IsEnabled = true;
                TestApiKeyButton.Content = "Test API Key";
            }
        }

        #endregion

        #region Event Handlers - Sliders

        private void TranslationTimeoutSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoading && TranslationTimeoutLabel != null)
            {
                TranslationTimeoutLabel.Text = $"{(int)e.NewValue}s";
            }
        }

        private void MaxRetryAttemptsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoading && MaxRetryAttemptsLabel != null)
            {
                MaxRetryAttemptsLabel.Text = ((int)e.NewValue).ToString();
            }
        }

        private void MaxTextLengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoading && MaxTextLengthLabel != null)
            {
                MaxTextLengthLabel.Text = ((int)e.NewValue).ToString();
            }
        }

        private void CacheExpireSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoading && CacheExpireLabel != null)
            {
                CacheExpireLabel.Text = $"{(int)e.NewValue}m";
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
            if (!_isLoading)
            {
                if (OverlayOpacityLabel != null)
                    OverlayOpacityLabel.Text = $"{(int)(e.NewValue * 100)}%";
                
                UpdateOverlayPreview();
            }
        }

        private void FontSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoading)
            {
                if (FontSizeLabel != null)
                    FontSizeLabel.Text = $"{(int)e.NewValue}pt";
                
                UpdateOverlayPreview();
            }
        }

        private void BorderThickness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoading)
            {
                if (BorderThicknessLabel != null)
                    BorderThicknessLabel.Text = $"{(int)e.NewValue}px";
                
                UpdateOverlayPreview();
            }
        }

        private void CornerRadius_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoading)
            {
                if (CornerRadiusLabel != null)
                    CornerRadiusLabel.Text = $"{(int)e.NewValue}px";
                
                UpdateOverlayPreview();
            }
        }

        #endregion

        #region Event Handlers - Hotkey

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

        #endregion

        #region Event Handlers - Colors

        private void BackgroundColor_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ShowColorPicker(BackgroundColorTextBox, BackgroundColorPreview);
        }

        private void TextColor_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ShowColorPicker(TextColorTextBox, TextColorPreview);
        }

        private void BorderColor_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ShowColorPicker(BorderColorTextBox, BorderColorPreview);
        }

        private void ShowColorPicker(TextBox textBox, Border preview)
        {
            var colorDialog = new System.Windows.Forms.ColorDialog();
            
            try
            {
                var currentColor = (Color)ColorConverter.ConvertFromString(textBox.Text);
                colorDialog.Color = System.Drawing.Color.FromArgb(currentColor.R, currentColor.G, currentColor.B);
            }
            catch
            {
                colorDialog.Color = System.Drawing.Color.White;
            }

            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var color = colorDialog.Color;
                var hexColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                textBox.Text = hexColor;
                
                UpdateColorPreviews();
                UpdateOverlayPreview();
            }
        }

        private void ColorTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isLoading)
            {
                UpdateColorPreviews();
                UpdateOverlayPreview();
            }
        }

        private void UpdateColorPreviews()
        {
            try
            {
                BackgroundColorPreview.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(BackgroundColorTextBox.Text));
            }
            catch { }

            try
            {
                TextColorPreview.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(TextColorTextBox.Text));
            }
            catch { }

            try
            {
                BorderColorPreview.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(BorderColorTextBox.Text));
            }
            catch { }
        }

        #endregion

        #region Event Handlers - Typography

        private void FontFamily_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoading)
            {
                UpdateOverlayPreview();
            }
        }

        #endregion

        #region Event Handlers - Checkboxes

        private void EnableClipboardMonitoring_Checked(object sender, RoutedEventArgs e)
        {
            UpdateClipboardMonitoringState();
        }

        private void EnableClipboardMonitoring_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateClipboardMonitoringState();
        }

        private void UpdateClipboardMonitoringState()
        {
            if (AutoTranslateOnSelectCheckBox != null)
            {
                AutoTranslateOnSelectCheckBox.IsEnabled = EnableClipboardMonitoringCheckBox.IsChecked == true;
                if (!AutoTranslateOnSelectCheckBox.IsEnabled)
                    AutoTranslateOnSelectCheckBox.IsChecked = false;
            }
        }

        private void EnableSoundNotifications_Checked(object sender, RoutedEventArgs e)
        {
            UpdateSoundSettingsPanelState();
        }

        private void EnableSoundNotifications_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateSoundSettingsPanelState();
        }

        private void UpdateSoundSettingsPanelState()
        {
            if (SoundSettingsPanel != null)
            {
                SoundSettingsPanel.IsEnabled = EnableSoundNotificationsCheckBox.IsChecked == true;
            }
        }

        #endregion

        #region Event Handlers - Sound

        private void BrowseSound_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Sound files (*.wav;*.mp3)|*.wav;*.mp3|All files (*.*)|*.*",
                Title = "Select notification sound"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                SoundFileTextBox.Text = openFileDialog.FileName;
            }
        }

        private void TestSound_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SoundFileTextBox.Text) || !File.Exists(SoundFileTextBox.Text))
            {
                MessageBox.Show("Please select a valid sound file first.", "No Sound File", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var player = new SoundPlayer(SoundFileTextBox.Text);
                player.Play();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to play sound: {ex.Message}", "Sound Test Failed", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Event Handlers - Advanced

        private void Theme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Theme changing logic would go here
            // For now, just store the selection
        }

        private void OpenConfigFolder_Click(object sender, RoutedEventArgs e)
        {
            var configDirectory = Path.GetDirectoryName(ConfigLocationTextBox.Text);
            if (Directory.Exists(configDirectory))
            {
                Process.Start("explorer.exe", configDirectory);
            }
        }

        private void BackupConfig_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                FileName = $"autotranslate-config-backup-{DateTime.Now:yyyy-MM-dd}.json",
                Title = "Save configuration backup"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    File.Copy(ConfigLocationTextBox.Text, saveFileDialog.FileName, true);
                    MessageBox.Show("Configuration backed up successfully!", "Backup Complete", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to backup configuration: {ex.Message}", "Backup Failed", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RestoreConfig_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Select configuration backup to restore"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var result = MessageBox.Show(
                    "This will overwrite your current configuration. Are you sure you want to continue?",
                    "Restore Configuration", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        File.Copy(openFileDialog.FileName, ConfigLocationTextBox.Text, true);
                        LoadSettings(); // Reload settings from restored file
                        MessageBox.Show("Configuration restored successfully!", "Restore Complete", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to restore configuration: {ex.Message}", "Restore Failed", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        #endregion

        #region Overlay Preview

        private void UpdateOverlayPreview()
        {
            if (_isLoading || OverlayPreview == null) return;

            try
            {
                // Update background
                OverlayPreview.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(BackgroundColorTextBox.Text));

                // Update border
                OverlayPreview.BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(BorderColorTextBox.Text));
                OverlayPreview.BorderThickness = new Thickness(BorderThicknessSlider.Value);
                OverlayPreview.CornerRadius = new CornerRadius(CornerRadiusSlider.Value);

                // Update opacity
                OverlayPreview.Opacity = OverlayOpacitySlider.Value;

                // Update shadow
                if (ShowDropShadowCheckBox.IsChecked == true)
                {
                    OverlayPreview.Effect = new DropShadowEffect
                    {
                        ShadowDepth = 3,
                        BlurRadius = 8,
                        Opacity = 0.3
                    };
                }
                else
                {
                    OverlayPreview.Effect = null;
                }

                // Update text
                if (PreviewText != null)
                {
                    PreviewText.Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString(TextColorTextBox.Text));
                    PreviewText.FontSize = FontSizeSlider.Value;
                    
                    if (FontFamilyCombo.SelectedItem != null)
                        PreviewText.FontFamily = new FontFamily(FontFamilyCombo.SelectedItem.ToString());
                }
            }
            catch
            {
                // Ignore color parsing errors during preview
            }
        }

        #endregion

        #region Button Handlers

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to reset all settings to defaults?", 
                "Reset Settings", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
            if (result == MessageBoxResult.Yes)
            {
                _isLoading = true;
                
                var defaultConfig = _configManager.GetFreshDefaultConfiguration();
                _configuration = defaultConfig;
                
                LoadSettings();
                
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
                
                _configuration.TranslationTimeoutSeconds = (int)TranslationTimeoutSlider.Value;
                _configuration.MaxRetryAttempts = (int)MaxRetryAttemptsSlider.Value;
                
                _configuration.StartMinimized = StartMinimizedCheckBox.IsChecked == true;
                _configuration.StartWithWindows = StartWithWindowsCheckBox.IsChecked == true;
                _configuration.MinimizeToTray = MinimizeToTrayCheckBox.IsChecked == true;
                _configuration.AutoRestoreClipboard = AutoRestoreClipboardCheckBox.IsChecked == true;
                _configuration.RememberWindowPosition = RememberWindowPositionCheckBox.IsChecked == true;
                _configuration.MaxTextLength = (int)MaxTextLengthSlider.Value;
                
                _configuration.EnableClipboardMonitoring = EnableClipboardMonitoringCheckBox.IsChecked == true;
                _configuration.AutoTranslateOnSelect = AutoTranslateOnSelectCheckBox.IsChecked == true;
                
                _configuration.EnableTextCaching = EnableTextCachingCheckBox.IsChecked == true;
                _configuration.PreloadCommonLanguages = PreloadCommonLanguagesCheckBox.IsChecked == true;
                _configuration.CacheExpireMinutes = (int)CacheExpireSlider.Value;
                
                _configuration.OverlayDurationSeconds = (int)OverlayDurationSlider.Value;
                _configuration.OverlayOpacity = OverlayOpacitySlider.Value;
                
                _configuration.OverlayBackgroundColor = BackgroundColorTextBox.Text;
                _configuration.OverlayTextColor = TextColorTextBox.Text;
                _configuration.OverlayBorderColor = BorderColorTextBox.Text;
                
                _configuration.OverlayFontFamily = FontFamilyCombo.SelectedItem?.ToString() ?? "Segoe UI";
                _configuration.OverlayFontSize = (int)FontSizeSlider.Value;
                
                _configuration.OverlayBorderThickness = (int)BorderThicknessSlider.Value;
                _configuration.OverlayCornerRadius = (int)CornerRadiusSlider.Value;
                _configuration.OverlayShowDropShadow = ShowDropShadowCheckBox.IsChecked == true;
                
                _configuration.EnableSoundNotifications = EnableSoundNotificationsCheckBox.IsChecked == true;
                _configuration.NotificationSoundPath = SoundFileTextBox.Text;
                _configuration.ShowNotificationOnSuccess = ShowNotificationOnSuccessCheckBox.IsChecked == true;
                _configuration.ShowNotificationOnError = ShowNotificationOnErrorCheckBox.IsChecked == true;
                
                _configuration.ShowLanguageFlags = ShowLanguageFlagsCheckBox.IsChecked == true;
                _configuration.CompactMode = CompactModeCheckBox.IsChecked == true;
                
                var selectedTheme = (ThemeCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Auto";
                _configuration.Theme = selectedTheme;

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

        #endregion
    }
}