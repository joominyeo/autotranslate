using System.Runtime.InteropServices;
using System.Windows;
using System.Diagnostics;

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

        [DllImport("user32.dll")]
        private static extern bool IsClipboardFormatAvailable(uint format);

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        private const int CF_UNICODETEXT = 13;
        private const byte VK_CONTROL = 0x11;
        private const byte VK_C = 0x43;
        private const uint KEYEVENTF_KEYUP = 0x02;
        private const int MAX_RETRY_ATTEMPTS = 3;
        private const int CLIPBOARD_TIMEOUT_MS = 1000;

        public async Task<TextCaptureResult> GetSelectedTextAsync()
        {
            return await Task.Run(async () =>
            {
                var result = new TextCaptureResult();
                string? originalClipboard = null;
                var retryCount = 0;

                try
                {
                    // Store current clipboard content with retry logic
                    originalClipboard = await GetClipboardTextWithRetryAsync();
                    
                    // Clear clipboard and wait for it to be actually cleared
                    await ClearClipboardWithRetryAsync();
                    
                    // Small delay to ensure clipboard is ready
                    await Task.Delay(50);
                    
                    // Send Ctrl+C to copy selected text
                    SendCtrlC();
                    
                    // Wait for clipboard to be updated with new content
                    string? selectedText = null;
                    var stopwatch = Stopwatch.StartNew();
                    
                    while (stopwatch.ElapsedMilliseconds < CLIPBOARD_TIMEOUT_MS)
                    {
                        await Task.Delay(50);
                        selectedText = GetClipboardText();
                        
                        // Check if we got new content (different from original)
                        if (!string.IsNullOrEmpty(selectedText) && selectedText != originalClipboard)
                        {
                            break;
                        }
                        
                        // If we still have the same content, wait a bit more
                        if (stopwatch.ElapsedMilliseconds > 200)
                        {
                            await Task.Delay(50);
                        }
                    }
                    
                    stopwatch.Stop();
                    
                    if (string.IsNullOrWhiteSpace(selectedText))
                    {
                        result.Success = false;
                        result.ErrorMessage = "No text was selected or copied to clipboard";
                        
                        // Restore original clipboard
                        if (!string.IsNullOrEmpty(originalClipboard))
                        {
                            await SetClipboardTextWithRetryAsync(originalClipboard);
                        }
                        
                        return result;
                    }

                    // Validate the captured text
                    if (selectedText.Length > 10000)
                    {
                        result.Success = false;
                        result.ErrorMessage = "Selected text is too long (max 10,000 characters)";
                        return result;
                    }

                    result.Success = true;
                    result.CapturedText = selectedText.Trim();
                    result.OriginalClipboard = originalClipboard;
                    
                    return result;
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Text capture failed: {ex.Message}";
                    
                    // Try to restore original clipboard on error
                    if (!string.IsNullOrEmpty(originalClipboard))
                    {
                        try
                        {
                            await SetClipboardTextWithRetryAsync(originalClipboard);
                        }
                        catch
                        {
                            // Ignore restoration errors
                        }
                    }
                    
                    return result;
                }
            });
        }

        public async Task RestoreClipboardAsync(string? originalContent)
        {
            if (!string.IsNullOrEmpty(originalContent))
            {
                await SetClipboardTextWithRetryAsync(originalContent);
            }
        }

        private void SendCtrlC()
        {
            // Press Ctrl
            keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
            
            // Press C
            keybd_event(VK_C, 0, 0, UIntPtr.Zero);
            
            // Small delay to ensure proper key registration
            Thread.Sleep(10);
            
            // Release C
            keybd_event(VK_C, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            
            // Release Ctrl
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        private async Task<string?> GetClipboardTextWithRetryAsync()
        {
            for (int i = 0; i < MAX_RETRY_ATTEMPTS; i++)
            {
                var text = GetClipboardText();
                if (text != null)
                    return text;
                
                if (i < MAX_RETRY_ATTEMPTS - 1)
                    await Task.Delay(100);
            }
            return null;
        }

        private async Task SetClipboardTextWithRetryAsync(string text)
        {
            for (int i = 0; i < MAX_RETRY_ATTEMPTS; i++)
            {
                if (SetClipboardText(text))
                    return;
                
                if (i < MAX_RETRY_ATTEMPTS - 1)
                    await Task.Delay(100);
            }
            throw new TextCaptureException("Failed to set clipboard text after multiple attempts");
        }

        private async Task ClearClipboardWithRetryAsync()
        {
            for (int i = 0; i < MAX_RETRY_ATTEMPTS; i++)
            {
                if (ClearClipboard())
                    return;
                
                if (i < MAX_RETRY_ATTEMPTS - 1)
                    await Task.Delay(100);
            }
        }

        private string? GetClipboardText()
        {
            try
            {
                if (OpenClipboard(IntPtr.Zero))
                {
                    try
                    {
                        if (IsClipboardFormatAvailable(CF_UNICODETEXT))
                        {
                            var data = GetClipboardData(CF_UNICODETEXT);
                            if (data != IntPtr.Zero)
                            {
                                var text = Marshal.PtrToStringUni(data);
                                return text;
                            }
                        }
                    }
                    finally
                    {
                        CloseClipboard();
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private bool SetClipboardText(string text)
        {
            try
            {
                if (OpenClipboard(IntPtr.Zero))
                {
                    try
                    {
                        EmptyClipboard();
                        
                        var ptr = Marshal.StringToHGlobalUni(text);
                        var success = SetClipboardData(CF_UNICODETEXT, ptr);
                        
                        if (!success)
                        {
                            Marshal.FreeHGlobal(ptr);
                        }
                        
                        return success;
                    }
                    finally
                    {
                        CloseClipboard();
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private bool ClearClipboard()
        {
            try
            {
                if (OpenClipboard(IntPtr.Zero))
                {
                    try
                    {
                        return EmptyClipboard();
                    }
                    finally
                    {
                        CloseClipboard();
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }

    public class TextCaptureResult
    {
        public bool Success { get; set; }
        public string CapturedText { get; set; } = string.Empty;
        public string? OriginalClipboard { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class TextCaptureException : Exception
    {
        public TextCaptureException(string message) : base(message) { }
        public TextCaptureException(string message, Exception innerException) : base(message, innerException) { }
    }
}