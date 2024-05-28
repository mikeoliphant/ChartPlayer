using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
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
        public bool LeftyMode { get; set; } = false;
        public bool RetuneToEStandard { get; set; } = false;
        public float NoteDisplaySeconds { get; set; } = 3;
        public ESongInstrumentType CurrentInstrument { get; set; } = ESongInstrumentType.LeadGuitar;
        public string SongListSortColumn { get; set; } = null;
        public bool SongListSortReversed { get; set; } = false;
    }

    public class SongPlayerSettingsInterface : InputDialog
    {
        public Action<SongPlayerSettings> ApplyAction { get; set; }

        SongPlayerSettings oldSettings;
        SongPlayerSettings newSettings = new SongPlayerSettings();

        TextBlock songPathText;

        public SongPlayerSettingsInterface(SongPlayerSettings settings)
            : base(Layout.Current.DefaultOutlineNinePatch)
        {
            oldSettings = settings;

            CopySettings(oldSettings, newSettings);

            UpdateDisplay();

            AddInput(new DialogInput { Text = "Apply", Action = Apply, CloseOnInput = true });
            AddInput(new DialogInput { Text = "Save as Default", Action = SaveDefault, CloseOnInput = false });
            AddInput(new DialogInput { Text = "Restore Default", Action = RestoreDefault, CloseOnInput = false });
            AddInput(new DialogInput { Text = "Cancel", CloseOnInput = true });
        }

        void UpdateDisplay()
        {
            VerticalStack vStack = new VerticalStack() { ChildSpacing = 10, HorizontalAlignment = EHorizontalAlignment.Stretch};
            SetContents(vStack);

            HorizontalStack songPathStack = new HorizontalStack()
            {
                HorizontalAlignment = EHorizontalAlignment.Stretch,
                ChildSpacing = 20
            };
            vStack.Children.Add(songPathStack);

            songPathStack.Children.Add(new TextBlock("Song Path:") { VerticalAlignment = EVerticalAlignment.Center });
            songPathStack.Children.Add(songPathText = new TextBlock(newSettings.SongPath)
            {
                HorizontalAlignment = EHorizontalAlignment.Stretch,
                VerticalAlignment = EVerticalAlignment.Center,
                TextColor = UIColor.Yellow
            });
            songPathStack.Children.Add(new TextButton("Select")
            {
                ClickAction = SelectSongPath
            });

            vStack.Children.Add(CreateTextToggleOption("InvertStrings", newSettings, "String Orientation:", "Low On Top", "Low On Bottom"));
            vStack.Children.Add(CreateTextToggleOption("LeftyMode", newSettings, "Guitar Orientation:", "Left Handed", "Right Handed"));
            vStack.Children.Add(CreateTextToggleOption("RetuneToEStandard", newSettings, "Re-tune to E Standard:", "Yes", "No"));
            vStack.Children.Add(CreateFloatOption("NoteDisplaySeconds", newSettings, "Note Display Length (secs):", 1, 5, 1));
        }

        UIElement CreateTextToggleOption(string property, object obj, string description, string option1, string option2)
        {
            PropertyInfo prop = obj.GetType().GetProperty(property);

            HorizontalStack hStack = new HorizontalStack()
            {
                HorizontalAlignment = EHorizontalAlignment.Stretch,
                ChildSpacing = 20
            };

            hStack.Children.Add(new TextBlock(description) { VerticalAlignment = EVerticalAlignment.Center });

            hStack.Children.Add(new UIElement { HorizontalAlignment = EHorizontalAlignment.Stretch });

            TextToggleButton button = new TextToggleButton(option1, option2) { ClickAction = delegate { prop.SetValue(obj, !(bool)prop.GetValue(obj)); } };
            button.SetPressed((bool)prop.GetValue(obj));
            hStack.Children.Add(button);

            return hStack;
        }

        UIElement CreateFloatOption(string property, object obj, string description, float minValue, float maxValue, int numDecimals)
        {
            PropertyInfo prop = obj.GetType().GetProperty(property);

            HorizontalStack hStack = new HorizontalStack()
            {
                HorizontalAlignment = EHorizontalAlignment.Stretch,
                ChildSpacing = 20
            };

            hStack.Children.Add(new TextBlock(description) { VerticalAlignment = EVerticalAlignment.Center });

            hStack.Children.Add(new UIElement { HorizontalAlignment = EHorizontalAlignment.Stretch });

            TextBlock numberText = new TextBlock { TextColor = UIColor.Yellow };
            hStack.Children.Add(numberText);

            string format = "n" + numDecimals;

            HorizontalSlider hSlider = new HorizontalSlider("HorizontalSlider")
            {
                VerticalAlignment = EVerticalAlignment.Center,
                DesiredWidth = 100,
                BackgroundColor = UIColor.Black,
                ChangeAction = delegate (float percent)
                {
                    float value = minValue + ((maxValue - minValue) * percent);
                    prop.SetValue(obj, value);
                    numberText.Text = value.ToString(format);
                }
            };

            float currentValue = (float)prop.GetValue(obj);

            numberText.Text = currentValue.ToString(format);
            hSlider.SetLevel((currentValue - minValue) / (maxValue - minValue));

            hStack.Children.Add(hSlider);

            return hStack;
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
                ApplyAction(newSettings);
        }

        void SaveDefault()
        {
            Apply();

            SongPlayerInterface.Instance.SaveDefaultOptions(newSettings);

            Layout.Current.UpdateLayout();
        }

        void RestoreDefault()
        {
            newSettings = SongPlayerInterface.Instance.LoadDefaultOptions();

            UpdateDisplay();

            Layout.Current.UpdateLayout();
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
