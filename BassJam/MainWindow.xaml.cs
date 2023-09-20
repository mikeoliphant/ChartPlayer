using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using Asio;
using AudioPlugSharp;

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

            if ((audioHost.AsioDriver == null) && PluginDisplay.ShowAudioSettingsWindow())
            {
                audioHost.SetAsioDriver(PluginDisplay.AsioDeviceName);
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            audioHost.Exit();

            base.OnClosing(e);
        }
    }
}
