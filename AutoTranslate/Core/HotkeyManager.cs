using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace AutoTranslate.Core
{
    public class HotkeyManager : IDisposable
    {
        private const int WM_HOTKEY = 0x0312;
        private const int WM_DESTROY = 0x0002;
        
        private readonly Window _window;
        private readonly Dictionary<int, Action> _hotkeyActions;
        private int _currentId = 1;
        private bool _disposed = false;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public HotkeyManager(Window window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _hotkeyActions = new Dictionary<int, Action>();
            
            var source = HwndSource.FromHwnd(new WindowInteropHelper(_window).Handle);
            source?.AddHook(HwndHook);
        }

        public void RegisterHotkey(ModifierKeys modifiers, Key key, Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            var id = _currentId++;
            var virtualKey = KeyInterop.VirtualKeyFromKey(key);
            var modifierFlags = GetModifierFlags(modifiers);
            
            var hwnd = new WindowInteropHelper(_window).Handle;
            
            if (!RegisterHotKey(hwnd, id, modifierFlags, (uint)virtualKey))
            {
                throw new InvalidOperationException($"Failed to register hotkey: {modifiers}+{key}");
            }

            _hotkeyActions[id] = action;
        }

        public void UnregisterAllHotkeys()
        {
            var hwnd = new WindowInteropHelper(_window).Handle;
            
            foreach (var id in _hotkeyActions.Keys.ToList())
            {
                UnregisterHotKey(hwnd, id);
            }
            
            _hotkeyActions.Clear();
        }

        private uint GetModifierFlags(ModifierKeys modifiers)
        {
            uint flags = 0;
            
            if (modifiers.HasFlag(ModifierKeys.Alt))
                flags |= 0x0001; // MOD_ALT
            if (modifiers.HasFlag(ModifierKeys.Control))
                flags |= 0x0002; // MOD_CONTROL
            if (modifiers.HasFlag(ModifierKeys.Shift))
                flags |= 0x0004; // MOD_SHIFT
            if (modifiers.HasFlag(ModifierKeys.Windows))
                flags |= 0x0008; // MOD_WIN
                
            return flags;
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                var id = wParam.ToInt32();
                if (_hotkeyActions.TryGetValue(id, out var action))
                {
                    try
                    {
                        action.Invoke();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error executing hotkey action: {ex.Message}", "Hotkey Error", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                handled = true;
            }
            
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                UnregisterAllHotkeys();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        ~HotkeyManager()
        {
            Dispose();
        }
    }

    [Flags]
    public enum HotkeyModifiers : uint
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Windows = 8
    }
}