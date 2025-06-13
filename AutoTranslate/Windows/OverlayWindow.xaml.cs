using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using System.Windows.Forms;
using AutoTranslate.Core;

namespace AutoTranslate
{
    public partial class OverlayWindow : Window
    {
        private readonly DispatcherTimer _autoCloseTimer;
        private readonly DispatcherTimer _countdownTimer;
        private readonly string _translatedText;
        private readonly string _sourceLanguage;
        private readonly string _targetLanguage;
        private int _timeoutSeconds = 5;
        private int _currentCountdown;

        public OverlayWindow(string originalText, string translatedText, string sourceLanguage = "", string targetLanguage = "")
        {
            InitializeComponent();
            
            _translatedText = translatedText;
            _sourceLanguage = sourceLanguage;
            _targetLanguage = targetLanguage;
            
            OriginalTextBlock.Text = originalText;
            TranslationTextBlock.Text = translatedText;
            
            // Set language information
            UpdateLanguageInfo();
            
            // Load configuration and apply appearance settings
            try
            {
                var config = new ConfigurationManager().LoadConfiguration();
                _timeoutSeconds = config.OverlayDurationSeconds > 0 ? config.OverlayDurationSeconds : 5;
                ApplyConfigurationToAppearance(config);
            }
            catch
            {
                _timeoutSeconds = 5; // Default fallback
            }
            
            _currentCountdown = _timeoutSeconds;
            
            // Auto-close timer
            _autoCloseTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(_timeoutSeconds)
            };
            _autoCloseTimer.Tick += (s, e) => {
                _autoCloseTimer.Stop();
                AnimateOut();
            };
            
            // Countdown timer for visual feedback
            _countdownTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _countdownTimer.Tick += CountdownTimer_Tick;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Position window near cursor with multi-monitor support
            PositionNearCursor();
            
            // Start timers
            _autoCloseTimer.Start();
            _countdownTimer.Start();
            
            // Animate window appearance
            AnimateIn();
            
            // Update initial countdown display
            UpdateCountdownDisplay();
        }

        private void PositionNearCursor()
        {
            try
            {
                var cursorPosition = GetCursorPosition();
                var targetScreen = Screen.FromPoint(new System.Drawing.Point((int)cursorPosition.X, (int)cursorPosition.Y));
                
                // Get screen bounds (working area to avoid taskbar)
                var workingArea = targetScreen.WorkingArea;
                
                // Calculate desired position (offset from cursor to avoid covering selected text)
                var offsetX = 25;
                var offsetY = 25;
                var left = cursorPosition.X + offsetX;
                var top = cursorPosition.Y + offsetY;
                
                // Ensure window fits within screen bounds
                if (left + ActualWidth > workingArea.Right)
                {
                    left = cursorPosition.X - ActualWidth - offsetX;
                    if (left < workingArea.Left)
                        left = workingArea.Right - ActualWidth - 20;
                }
                
                if (top + ActualHeight > workingArea.Bottom)
                {
                    top = cursorPosition.Y - ActualHeight - offsetY;
                    if (top < workingArea.Top)
                        top = workingArea.Bottom - ActualHeight - 20;
                }
                
                // Final bounds check
                left = Math.Max(workingArea.Left + 10, Math.Min(left, workingArea.Right - ActualWidth - 10));
                top = Math.Max(workingArea.Top + 10, Math.Min(top, workingArea.Bottom - ActualHeight - 10));
                
                Left = left;
                Top = top;
            }
            catch
            {
                // Fallback to center of primary screen
                var primaryScreen = Screen.PrimaryScreen.WorkingArea;
                Left = primaryScreen.Left + (primaryScreen.Width - ActualWidth) / 2;
                Top = primaryScreen.Top + (primaryScreen.Height - ActualHeight) / 2;
            }
        }

        private Point GetCursorPosition()
        {
            var point = new System.Drawing.Point();
            if (GetCursorPos(out point))
            {
                return new Point(point.X, point.Y);
            }
            return new Point(0, 0);
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool GetCursorPos(out System.Drawing.Point lpPoint);

        private void UpdateLanguageInfo()
        {
            try
            {
                var sourceLanguageName = string.IsNullOrEmpty(_sourceLanguage) ? "Unknown" : 
                    LanguageManager.GetLanguageName(_sourceLanguage);
                var targetLanguageName = string.IsNullOrEmpty(_targetLanguage) ? "Unknown" : 
                    LanguageManager.GetLanguageName(_targetLanguage);
                
                LanguageInfoTextBlock.Text = $"{sourceLanguageName} → {targetLanguageName}";
            }
            catch
            {
                LanguageInfoTextBlock.Text = "Translation";
            }
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            _currentCountdown--;
            UpdateCountdownDisplay();
            
            if (_currentCountdown <= 0)
            {
                _countdownTimer.Stop();
            }
        }

        private void UpdateCountdownDisplay()
        {
            if (_currentCountdown > 0)
            {
                TimeoutIndicator.Text = $"⏱ {_currentCountdown}s";
            }
            else
            {
                TimeoutIndicator.Text = "";
            }
        }

        private void AnimateIn()
        {
            // Enhanced fade-in and scale animation
            Opacity = 0;
            Transform = new System.Windows.Media.ScaleTransform(0.8, 0.8);
            
            var storyboard = new Storyboard();
            
            // Fade in
            var fadeAnimation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(fadeAnimation, this);
            Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(fadeAnimation);
            
            // Scale up
            var scaleXAnimation = new DoubleAnimation(0.8, 1.0, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new System.Windows.Media.Animation.BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
            };
            var scaleYAnimation = new DoubleAnimation(0.8, 1.0, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new System.Windows.Media.Animation.BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
            };
            
            Storyboard.SetTarget(scaleXAnimation, this);
            Storyboard.SetTargetProperty(scaleXAnimation, new PropertyPath("RenderTransform.ScaleX"));
            Storyboard.SetTarget(scaleYAnimation, this);
            Storyboard.SetTargetProperty(scaleYAnimation, new PropertyPath("RenderTransform.ScaleY"));
            
            storyboard.Children.Add(scaleXAnimation);
            storyboard.Children.Add(scaleYAnimation);
            
            RenderTransform = new System.Windows.Media.ScaleTransform();
            RenderTransformOrigin = new Point(0.5, 0.5);
            
            storyboard.Begin();
        }

        private void AnimateOut()
        {
            var storyboard = new Storyboard();
            
            // Fade out
            var fadeAnimation = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200))
            {
                EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            Storyboard.SetTarget(fadeAnimation, this);
            Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(fadeAnimation);
            
            storyboard.Completed += (s, e) => Close();
            storyboard.Begin();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                // Allow dragging the window
                try
                {
                    DragMove();
                }
                catch
                {
                    // Handle potential exceptions during drag
                }
                
                // Reset timers when interacting
                RestartTimers();
            }
        }

        private void RestartTimers()
        {
            _autoCloseTimer.Stop();
            _countdownTimer.Stop();
            
            _currentCountdown = _timeoutSeconds;
            UpdateCountdownDisplay();
            
            _autoCloseTimer.Start();
            _countdownTimer.Start();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            StopAllTimers();
            AnimateOut();
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Clipboard.SetText(_translatedText);
                
                // Show brief confirmation with animation
                var originalContent = CopyButton.Content;
                CopyButton.Content = "✓ Copied!";
                CopyButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80));
                
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
                timer.Tick += (s, args) => {
                    CopyButton.Content = originalContent;
                    CopyButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(70, 130, 180));
                    timer.Stop();
                };
                timer.Start();
                
                // Reset timers when interacting
                RestartTimers();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to copy text: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopAllTimers()
        {
            _autoCloseTimer?.Stop();
            _countdownTimer?.Stop();
        }

        protected override void OnClosed(EventArgs e)
        {
            StopAllTimers();
            base.OnClosed(e);
        }

        private void ApplyConfigurationToAppearance(AppConfiguration config)
        {
            try
            {
                // Apply opacity
                Opacity = config.OverlayOpacity;

                // Apply colors
                var backgroundColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(config.OverlayBackgroundColor);
                var textColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(config.OverlayTextColor);
                var borderColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(config.OverlayBorderColor);

                Background = new System.Windows.Media.SolidColorBrush(backgroundColor);
                BorderBrush = new System.Windows.Media.SolidColorBrush(borderColor);
                BorderThickness = new Thickness(config.OverlayBorderThickness);

                // Apply corner radius
                var radius = new CornerRadius(config.OverlayCornerRadius);
                // Note: CornerRadius property might need to be applied to a Border element instead

                // Apply typography
                FontFamily = new System.Windows.Media.FontFamily(config.OverlayFontFamily);
                FontSize = config.OverlayFontSize;

                // Apply text color to text blocks
                var textBrush = new System.Windows.Media.SolidColorBrush(textColor);
                if (OriginalTextBlock != null) OriginalTextBlock.Foreground = textBrush;
                if (TranslationTextBlock != null) TranslationTextBlock.Foreground = textBrush;
                if (LanguageInfoTextBlock != null) LanguageInfoTextBlock.Foreground = textBrush;
                if (TimeoutIndicator != null) TimeoutIndicator.Foreground = textBrush;

                // Apply drop shadow if enabled
                if (config.OverlayShowDropShadow)
                {
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        ShadowDepth = 3,
                        BlurRadius = 8,
                        Opacity = 0.3,
                        Color = System.Windows.Media.Colors.Black
                    };
                }
                else
                {
                    Effect = null;
                }
            }
            catch (Exception ex)
            {
                // If there's an error applying configuration, just use defaults
                System.Diagnostics.Debug.WriteLine($"Error applying overlay appearance configuration: {ex.Message}");
            }
        }
    }
}