using System.Configuration;
using System.Data;
using System.Windows;

namespace AutoTranslate
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Ensure only one instance is running
            var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            var processes = System.Diagnostics.Process.GetProcessesByName(currentProcess.ProcessName);
            
            if (processes.Length > 1)
            {
                MessageBox.Show("AutoTranslate is already running.", "AutoTranslate", MessageBoxButton.OK, MessageBoxImage.Information);
                Current.Shutdown();
                return;
            }
        }
    }
}