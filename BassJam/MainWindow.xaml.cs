using System;
using System.Windows;
using System.Windows.Interop;
using Asio;

namespace BassJam
{
    public partial class MainWindow : System.Windows.Window
    {
        AudioPlugSharpHost<BassJamPlugin> audioHost = new AudioPlugSharpHost<BassJamPlugin>(new BassJamPlugin());

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            PluginDisplay.SetPlugin(audioHost.Plugin);

            if (PluginDisplay.ShowAudioSettingsWindow())
            {
                audioHost.SetAsioDriver(new AsioDriver(PluginDisplay.AsioDeviceName));
            }
        }
    }
}
