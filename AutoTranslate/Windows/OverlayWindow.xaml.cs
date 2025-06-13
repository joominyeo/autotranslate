using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace AutoTranslate
{
    public partial class OverlayWindow : Window
    {
        private readonly DispatcherTimer _autoCloseTimer;
        private readonly string _translatedText;

        public OverlayWindow(string originalText, string translatedText)
        {
            InitializeComponent();
            
            _translatedText = translatedText;
            
            OriginalTextBlock.Text = originalText;
            TranslationTextBlock.Text = translatedText;
            
            // Auto-close timer
            _autoCloseTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(8)
            };
            _autoCloseTimer.Tick += (s, e) => {
                _autoCloseTimer.Stop();
                Close();
            };
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Position window near cursor
            PositionNearCursor();
            
            // Start auto-close timer
            _autoCloseTimer.Start();
            
            // Animate window appearance
            AnimateIn();
        }

        private void PositionNearCursor()
        {
            try
            {
                var cursorPosition = GetCursorPosition();
                var screenWidth = SystemParameters.PrimaryScreenWidth;
                var screenHeight = SystemParameters.PrimaryScreenHeight;
                
                // Calculate desired position (offset from cursor)
                var left = cursorPosition.X + 20;
                var top = cursorPosition.Y + 20;
                
                // Ensure window stays on screen
                if (left + Width > screenWidth)
                    left = screenWidth - Width - 10;
                if (top + Height > screenHeight)
                    top = cursorPosition.Y - Height - 10;
                
                // Minimum bounds
                if (left < 0) left = 10;
                if (top < 0) top = 10;
                
                Left = left;
                Top = top;
            }
            catch
            {
                // Fallback to center screen
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
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

        private void AnimateIn()
        {
            // Simple fade-in animation
            Opacity = 0;
            var animation = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            BeginAnimation(OpacityProperty, animation);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                // Allow dragging the window
                DragMove();
                
                // Reset auto-close timer when interacting
                _autoCloseTimer.Stop();
                _autoCloseTimer.Start();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _autoCloseTimer.Stop();
            Close();
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(_translatedText);
                
                // Show brief confirmation
                CopyButton.Content = "Copied!";
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                timer.Tick += (s, args) => {
                    CopyButton.Content = "Copy";
                    timer.Stop();
                };
                timer.Start();
                
                // Reset auto-close timer
                _autoCloseTimer.Stop();
                _autoCloseTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy text: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _autoCloseTimer?.Stop();
            base.OnClosed(e);
        }
    }
}