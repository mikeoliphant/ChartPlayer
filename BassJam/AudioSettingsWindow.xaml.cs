using Asio;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BassJam
{
    /// <summary>
    /// Interaction logic for AudioSettingsWindow.xaml
    /// </summary>
    public partial class AudioSettingsWindow : Window
    {
        public string AsioDeviceName { get; set; }

        public AudioSettingsWindow()
        {
            InitializeComponent();

            foreach (var entry in AsioDriver.GetAsioDriverEntries())
            {
                AsioCombo.Items.Add(entry.Name);
            }

            AsioCombo.SelectedIndex = 0;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.AsioDeviceName = AsioCombo.SelectedValue.ToString();

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
