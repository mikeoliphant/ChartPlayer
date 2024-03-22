using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using SongFormat;
using UILayout;

namespace ChartPlayer
{
    public class SongPlayerSettings
    {
        public string SongPath { get; set; } = null;
        public bool InvertStrings { get; set; } = false;
        public bool RetuneToEStandard { get; set; } = true;
        public ESongInstrumentType CurrentInstrument { get; set; } = ESongInstrumentType.LeadGuitar;
    }

    public class SongPlayerSettingsInterface : InputDialog
    {
        public Action ApplyAction { get; set; }

        SongPlayerSettings oldSettings;
        SongPlayerSettings newSettings = new SongPlayerSettings();

        TextBlock songPathText;

        public SongPlayerSettingsInterface(SongPlayerSettings settings)
            : base(Layout.Current.DefaultOutlineNinePatch)
        {
            oldSettings = settings;

            CopySettings(oldSettings, newSettings);

            VerticalStack vStack = new VerticalStack() { ChildSpacing = 10, VerticalAlignment = EVerticalAlignment.Stretch };
            SetContents(vStack);

            HorizontalStack songPathStack = new HorizontalStack()
            {
                HorizontalAlignment = EHorizontalAlignment.Stretch,
                ChildSpacing = 20
            };
            vStack.Children.Add(songPathStack);

            songPathStack.Children.Add(new TextBlock("Song Path:") { VerticalAlignment = EVerticalAlignment.Center });
            songPathStack.Children.Add(songPathText = new TextBlock(settings.SongPath)
            {
                HorizontalAlignment = EHorizontalAlignment.Stretch,
                VerticalAlignment = EVerticalAlignment.Center
            });
            songPathStack.Children.Add(new TextButton("Select")
            {
                ClickAction = SelectSongPath
            });

            HorizontalStack invertStack = new HorizontalStack()
            {
                HorizontalAlignment = EHorizontalAlignment.Stretch,
                ChildSpacing = 20
            };
            vStack.Children.Add(invertStack);

            invertStack.Children.Add(new TextBlock("String Orientation:"){ VerticalAlignment = EVerticalAlignment.Center });

            invertStack.Children.Add(new UIElement { HorizontalAlignment = EHorizontalAlignment.Stretch });

            TextToggleButton invertStringsButton = new TextToggleButton("Low On Top", "Low On Bottom") { ClickAction = delegate { newSettings.InvertStrings = !newSettings.InvertStrings; } };
            invertStringsButton.SetPressed(newSettings.InvertStrings);
            invertStack.Children.Add(invertStringsButton);

            HorizontalStack retuneStack = new HorizontalStack()
            {
                HorizontalAlignment = EHorizontalAlignment.Stretch,
                ChildSpacing = 20
            };
            vStack.Children.Add(retuneStack);

            retuneStack.Children.Add(new TextBlock("Re-tune to E Standard:") { VerticalAlignment = EVerticalAlignment.Center });

            retuneStack.Children.Add(new UIElement { HorizontalAlignment = EHorizontalAlignment.Stretch });

            TextToggleButton retuneButton = new TextToggleButton("Yes", "No")
            {                
                ClickAction = delegate { newSettings.RetuneToEStandard = !newSettings.RetuneToEStandard; }
            };
            retuneButton.SetPressed(newSettings.RetuneToEStandard);
            retuneStack.Children.Add(retuneButton);

            AddInput(new DialogInput { Text = "Apply", Action = Apply, CloseOnInput = true });
            AddInput(new DialogInput { Text = "Cancel", CloseOnInput = true });
        }

        void CopySettings(SongPlayerSettings fromSettings, SongPlayerSettings toSettings)
        {
            foreach (PropertyInfo property in typeof(SongPlayerSettings).GetProperties().Where(p => p.CanWrite))
            {
                property.SetValue(toSettings, property.GetValue(fromSettings, null), null);
            }
        }

        void Apply()
        {
            CopySettings(newSettings, oldSettings);

            if (ApplyAction != null)
                ApplyAction();
        }

        void SelectSongPath()
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();

            dialog.SelectedPath = newSettings.SongPath;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                newSettings.SongPath = dialog.SelectedPath;

                songPathText.Text = newSettings.SongPath;

                Layout.Current.UpdateLayout();
            }
        }
    }
}
