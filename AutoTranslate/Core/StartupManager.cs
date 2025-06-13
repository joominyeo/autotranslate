using Microsoft.Win32;
using System.Diagnostics;

namespace AutoTranslate.Core
{
    public static class StartupManager
    {
        private const string APP_NAME = "AutoTranslate";
        private const string REGISTRY_KEY_PATH = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        
        public static bool IsStartupEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY_PATH, false);
                var value = key?.GetValue(APP_NAME);
                return value != null;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to check startup status: {ex.Message}", 
                    "Registry Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return false;
            }
        }
        
        public static bool SetStartupEnabled(bool enabled)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY_PATH, true);
                if (key == null)
                {
                    System.Windows.MessageBox.Show("Unable to access Windows startup registry key.", 
                        "Registry Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return false;
                }
                
                if (enabled)
                {
                    var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                    if (string.IsNullOrEmpty(exePath))
                    {
                        System.Windows.MessageBox.Show("Unable to determine application path.", 
                            "Path Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        return false;
                    }
                    
                    key.SetValue(APP_NAME, $"\"{exePath}\" --minimized");
                }
                else
                {
                    key.DeleteValue(APP_NAME, false);
                }
                
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                System.Windows.MessageBox.Show(
                    "Access denied. Please run the application as administrator to change startup settings.", 
                    "Permission Required", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return false;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to update startup settings: {ex.Message}", 
                    "Registry Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return false;
            }
        }
        
        public static void SyncWithConfiguration(AppConfiguration config)
        {
            if (IsStartupEnabled() != config.StartWithWindows)
            {
                SetStartupEnabled(config.StartWithWindows);
            }
        }
    }
}