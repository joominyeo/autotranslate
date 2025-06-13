using System.Windows;
using System.Windows.Input;

namespace AutoTranslate.Core
{
    public partial class HotkeyDialog : Window
    {
        public ModifierKeys Modifiers { get; private set; }
        public Key Key { get; private set; }
        public string HotkeyText { get; private set; } = "";

        public HotkeyDialog()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Focus();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            
            // Ignore modifier keys by themselves
            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LWin || key == Key.RWin)
            {
                return;
            }

            var modifiers = Keyboard.Modifiers;
            
            // Require at least one modifier key
            if (modifiers == ModifierKeys.None)
            {
                HotkeyDisplay.Text = "Press a key combination with Ctrl, Alt, Shift, or Win";
                OkButton.IsEnabled = false;
                return;
            }

            Modifiers = modifiers;
            Key = key;
            HotkeyText = BuildHotkeyText(modifiers, key);
            
            HotkeyDisplay.Text = HotkeyText;
            OkButton.IsEnabled = true;
        }

        private string BuildHotkeyText(ModifierKeys modifiers, Key key)
        {
            var parts = new List<string>();
            
            if (modifiers.HasFlag(ModifierKeys.Control))
                parts.Add("Ctrl");
            if (modifiers.HasFlag(ModifierKeys.Alt))
                parts.Add("Alt");
            if (modifiers.HasFlag(ModifierKeys.Shift))
                parts.Add("Shift");
            if (modifiers.HasFlag(ModifierKeys.Windows))
                parts.Add("Win");
                
            parts.Add(key.ToString());
            
            return string.Join(" + ", parts);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}