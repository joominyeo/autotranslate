using System.Runtime.InteropServices;
using System.Windows;

namespace AutoTranslate.Services
{
    public class TextCapture
    {
        [DllImport("user32.dll")]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        private static extern bool EmptyClipboard();

        [DllImport("user32.dll")]
        private static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("user32.dll")]
        private static extern bool SetClipboardData(uint uFormat, IntPtr hMem);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const int CF_UNICODETEXT = 13;
        private const byte VK_CONTROL = 0x11;
        private const byte VK_C = 0x43;
        private const uint KEYEVENTF_KEYUP = 0x02;

        public async Task<string> GetSelectedTextAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Store current clipboard content
                    var originalClipboard = GetClipboardText();
                    
                    // Clear clipboard
                    ClearClipboard();
                    
                    // Wait a moment
                    Thread.Sleep(50);
                    
                    // Send Ctrl+C to copy selected text
                    SendCtrlC();
                    
                    // Wait for clipboard to be updated
                    Thread.Sleep(100);
                    
                    // Get the copied text
                    var selectedText = GetClipboardText();
                    
                    // Restore original clipboard if we didn't get new content
                    if (string.IsNullOrEmpty(selectedText) && !string.IsNullOrEmpty(originalClipboard))
                    {
                        SetClipboardText(originalClipboard);
                    }
                    
                    return selectedText ?? string.Empty;
                }
                catch (Exception ex)
                {
                    throw new TextCaptureException($"Failed to capture selected text: {ex.Message}", ex);
                }
            });
        }

        private void SendCtrlC()
        {
            // Press Ctrl
            keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
            
            // Press C
            keybd_event(VK_C, 0, 0, UIntPtr.Zero);
            
            // Release C
            keybd_event(VK_C, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            
            // Release Ctrl
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        private string? GetClipboardText()
        {
            try
            {
                if (OpenClipboard(IntPtr.Zero))
                {
                    var data = GetClipboardData(CF_UNICODETEXT);
                    if (data != IntPtr.Zero)
                    {
                        var text = Marshal.PtrToStringUni(data);
                        CloseClipboard();
                        return text;
                    }
                    CloseClipboard();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private void SetClipboardText(string text)
        {
            try
            {
                if (OpenClipboard(IntPtr.Zero))
                {
                    EmptyClipboard();
                    
                    var ptr = Marshal.StringToHGlobalUni(text);
                    SetClipboardData(CF_UNICODETEXT, ptr);
                    
                    CloseClipboard();
                }
            }
            catch
            {
                // Ignore errors when setting clipboard
            }
        }

        private void ClearClipboard()
        {
            try
            {
                if (OpenClipboard(IntPtr.Zero))
                {
                    EmptyClipboard();
                    CloseClipboard();
                }
            }
            catch
            {
                // Ignore errors when clearing clipboard
            }
        }
    }

    public class TextCaptureException : Exception
    {
        public TextCaptureException(string message) : base(message) { }
        public TextCaptureException(string message, Exception innerException) : base(message, innerException) { }
    }
}