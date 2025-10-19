using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Algoloop.Avalonia.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void FileSettings(object? sender, RoutedEventArgs e)
        {
            var settingsView = new SettingsView();
            _ = settingsView.ShowDialog(this);
        }

        private void HelpAbout(object? sender, RoutedEventArgs e)
        {
            var about = new AboutView();
            _ = about.ShowDialog(this);
        }

        private void HelpDocumentation(object? sender, RoutedEventArgs e)
        {
            OpenUrl("https://github.com/Capnode/Algoloop/wiki/Documentation");
        }

        private void HelpTechnicalSupport(object? sender, RoutedEventArgs e)
        {
            OpenUrl("https://github.com/Capnode/Algoloop/issues");
        }

        private void HelpPrivacyPolicy(object? sender, RoutedEventArgs e)
        {
            OpenUrl("https://github.com/Capnode/Algoloop/wiki/Privacy-policy");
        }

        private void OnExit(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private static void OpenUrl(string url)
        {
            try
            {
                // Cross-platform URL opening
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    var psi = new ProcessStartInfo("cmd", $"/c start {url}")
                    {
                        CreateNoWindow = true
                    };
                    Process.Start(psi);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{ex.GetType()}: {ex.Message}");
            }
        }

        private void OnTheme(object? sender, RoutedEventArgs e)
        {
            // Theme switching would be implemented here for Avalonia
            // This is a placeholder for future theme support
        }
    }
}
