using System.Windows;
using AutoTranslate.Core;

namespace AutoTranslate.Windows
{
    public partial class HelpWindow : Window
    {
        public HelpWindow()
        {
            InitializeComponent();
            LoadHotkeyInformation();
        }

        private void LoadHotkeyInformation()
        {
            try
            {
                var configManager = new ConfigurationManager();
                var config = configManager.LoadConfiguration();
                
                // Update hotkey displays
                HotkeyTextBlock.Text = config.HotkeyDisplayText;
                GlobalHotkeyTextBlock.Text = config.HotkeyDisplayText;
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading hotkey information in help window", ex);
                // Use defaults if loading fails
                HotkeyTextBlock.Text = "Ctrl + Shift + T";
                GlobalHotkeyTextBlock.Text = "Ctrl + Shift + T";
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}