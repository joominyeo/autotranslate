using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Linq;
using AutoTranslate.Core;
using AutoTranslate.Services;

namespace AutoTranslate
{
    public partial class MainWindow : Window
    {
        private readonly HotkeyManager _hotkeyManager;
        private readonly ConfigurationManager _configManager;
        private readonly TranslationService _translationService;
        private OverlayWindow? _overlayWindow;
        private bool _isClosing = false;

        public MainWindow()
        {
            InitializeComponent();
            
            _hotkeyManager = new HotkeyManager(this);
            _configManager = new ConfigurationManager();
            _translationService = new TranslationService();
            
            InitializeUI();
            LoadSettings();
            RegisterHotkeys();
        }

        private void InitializeUI()
        {
            // Initialize language options using the comprehensive language manager
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
            var config = _configManager.LoadConfiguration();
            
            SourceLanguageCombo.SelectedValue = config.SourceLanguage;
            TargetLanguageCombo.SelectedValue = config.TargetLanguage;
            HotkeyTextBox.Text = config.HotkeyDisplayText;
            StartMinimizedCheckBox.IsChecked = config.StartMinimized;

            if (config.StartMinimized)
            {
                WindowState = WindowState.Minimized;
                Hide();
            }

            LogMessage("AutoTranslate started successfully.");
        }

        private void RegisterHotkeys()
        {
            var config = _configManager.LoadConfiguration();
            try
            {
                _hotkeyManager.RegisterHotkey(config.HotkeyModifiers, config.HotkeyKey, OnTranslationHotkeyPressed);
                LogMessage($"Hotkey registered: {config.HotkeyDisplayText}");
            }
            catch (Exception ex)
            {
                LogMessage($"Failed to register hotkey: {ex.Message}");
            }
        }

        private async void OnTranslationHotkeyPressed()
        {
            try
            {
                var config = _configManager.LoadConfiguration();
                var textCapture = new TextCapture();
                
                LogMessage("Capturing selected text...");
                var captureResult = await textCapture.GetSelectedTextAsync();

                if (!captureResult.Success)
                {
                    LogMessage($"Text capture failed: {captureResult.ErrorMessage}");
                    return;
                }

                var selectedText = captureResult.CapturedText;
                if (string.IsNullOrWhiteSpace(selectedText))
                {
                    LogMessage("No text was selected.");
                    return;
                }

                LogMessage($"Captured text: {selectedText.Substring(0, Math.Min(50, selectedText.Length))}...");

                // Perform translation with enhanced workflow
                var translationResult = await _translationService.TranslateAsync(
                    selectedText, 
                    config.SourceLanguage, 
                    config.TargetLanguage);

                if (translationResult.Success)
                {
                    var sourceLanguageName = LanguageManager.GetLanguageName(translationResult.SourceLanguage);
                    var targetLanguageName = LanguageManager.GetLanguageName(translationResult.TargetLanguage);
                    
                    ShowTranslationOverlay(selectedText, translationResult);
                    
                    var methodInfo = string.IsNullOrEmpty(translationResult.Method) ? "" : $" ({translationResult.Method})";
                    LogMessage($"Translated from {sourceLanguageName} to {targetLanguageName}{methodInfo}: {translationResult.TranslatedText}");
                    
                    if (!string.IsNullOrEmpty(translationResult.DetectedSourceLanguage) && 
                        translationResult.DetectedSourceLanguage != config.SourceLanguage)
                    {
                        var detectedLanguageName = LanguageManager.GetLanguageName(translationResult.DetectedSourceLanguage);
                        LogMessage($"Detected source language: {detectedLanguageName}");
                    }
                }
                else
                {
                    LogMessage($"Translation failed: {translationResult.ErrorMessage}");
                }

                // Restore clipboard if configured
                if (config.AutoRestoreClipboard && !string.IsNullOrEmpty(captureResult.OriginalClipboard))
                {
                    await textCapture.RestoreClipboardAsync(captureResult.OriginalClipboard);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Translation error: {ex.Message}");
            }
        }

        private void ShowTranslationOverlay(string originalText, TranslationResult translationResult)
        {
            _overlayWindow?.Close();
            _overlayWindow = new OverlayWindow(originalText, translationResult.TranslatedText, 
                translationResult.SourceLanguage, translationResult.TargetLanguage);
            _overlayWindow.Show();
        }

        private void LogMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                LogTextBlock.Text += $"[{timestamp}] {message}\n";
            });
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
                MyNotifyIcon.ShowBalloonTip("AutoTranslate", "Application minimized to tray", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!_isClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }

            _hotkeyManager?.Dispose();
            _overlayWindow?.Close();
        }

        private void MyNotifyIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void MenuOpen_Click(object sender, RoutedEventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void MenuHide_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void MenuSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new Windows.SettingsWindow();
            settingsWindow.Owner = this;
            
            if (settingsWindow.ShowDialog() == true)
            {
                // Reload settings and update UI
                LoadSettings();
                RegisterHotkeys();
                LogMessage("Settings updated successfully.");
            }
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            _isClosing = true;
            Close();
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("AutoTranslate v1.0\nReal-time text translation with global hotkeys\n\nPress your configured hotkey while text is selected to translate it.", 
                "About AutoTranslate", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SetHotkey_Click(object sender, RoutedEventArgs e)
        {
            var hotkeyDialog = new HotkeyDialog();
            if (hotkeyDialog.ShowDialog() == true)
            {
                try
                {
                    _hotkeyManager.UnregisterAllHotkeys();
                    _hotkeyManager.RegisterHotkey(hotkeyDialog.Modifiers, hotkeyDialog.Key, OnTranslationHotkeyPressed);
                    
                    HotkeyTextBox.Text = hotkeyDialog.HotkeyText;
                    LogMessage($"Hotkey changed to: {hotkeyDialog.HotkeyText}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to set hotkey: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var config = new AppConfiguration
                {
                    SourceLanguage = SourceLanguageCombo.SelectedValue?.ToString() ?? "auto",
                    TargetLanguage = TargetLanguageCombo.SelectedValue?.ToString() ?? "en",
                    HotkeyDisplayText = HotkeyTextBox.Text,
                    StartMinimized = StartMinimizedCheckBox.IsChecked == true
                };

                _configManager.SaveConfiguration(config);
                LogMessage("Settings saved successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            LogTextBlock.Text = "";
        }
    }
}