using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Media;
using System.Reflection;
using SongFormat;
using UILayout;

namespace ChartPlayer
{
    public enum ESongTuningMode
    {
        None,
        A440,
        EStandard,
        EbStandard,
        DStandard,
        CSharpStandard,
        CStandard,
        BStandard
    }

    public class SongPlayerSettings
    {
        public string SongPath { get; set; } = null;
        public bool InvertStrings { get; set; } = false;
        public bool LeftyMode { get; set; } = false;
        public ESongTuningMode SongTuningMode { get; set; } = ESongTuningMode.A440;
        public float NoteDisplaySeconds { get; set; } = 3;
        public float DrumsNoteDisplaySeconds { get; set; } = 3;
        public float KeysNoteDisplaySeconds { get; set; } = 3;
        public ESongInstrumentType CurrentInstrument { get; set; } = ESongInstrumentType.LeadGuitar;
        public string SongListSortColumn { get; set; } = null;
        public bool SongListSortReversed { get; set; } = false;
        public float UIScale { get; set; } = 1.0f;
    }

    public class SongPlayerSettingsInterface : InputDialog
    {
        public Action<SongPlayerSettings> ApplyAction { get; set; }

        SongPlayerSettings oldSettings;
        SongPlayerSettings newSettings = new SongPlayerSettings();

        TextBlock songPathText;
        MidiEditor midiEditor = new MidiEditor();

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
            TabPanel tabPanel = new TabPanel(ChartPlayerGame.PanelBackgroundColorDark, UIColor.White, Layout.Current.GetImage("TabPanelBackground"), Layout.Current.GetImage("TabForeground"), Layout.Current.GetImage("TabBackground"), 5, 5);
            SetContents(tabPanel);


            tabPanel.AddTab("General", GeneralTab());
            tabPanel.AddTab("Guitar", GuitarTab());
            tabPanel.AddTab("Drums", DrumsTab());
        }

        UIElement GeneralTab()
        {
            VerticalStack vStack = new VerticalStack() { ChildSpacing = 10, HorizontalAlignment = EHorizontalAlignment.Stretch };

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

            vStack.Children.Add(CreateTextToggleOption("LeftyMode", newSettings, "Guitar Orientation:", "Left Handed", "Right Handed"));
            vStack.Children.Add(CreateFloatOption("UIScale", newSettings, "User Interface Scale", 0.25f, 3.0f, 2));

            return vStack;
        }

        UIElement GuitarTab()
        {
            VerticalStack vStack = new VerticalStack() { ChildSpacing = 10, HorizontalAlignment = EHorizontalAlignment.Stretch };

            vStack.Children.Add(CreateTextToggleOption("InvertStrings", newSettings, "String Orientation:", "Low On Top", "Low On Bottom"));
            vStack.Children.Add(CreateEnumOption("SongTuningMode", newSettings, "Song Re-Tuning"));
            vStack.Children.Add(CreateFloatOption("NoteDisplaySeconds", newSettings, "Note Display Length (secs):", 1, 5, 1));

            return vStack;
        }

        UIElement DrumsTab()
        {
            VerticalStack vStack = new VerticalStack() { ChildSpacing = 10, HorizontalAlignment = EHorizontalAlignment.Stretch };

            vStack.Children.Add(CreateFloatOption("DrumsNoteDisplaySeconds", newSettings, "Note Display Length (secs):", 1, 5, 1));
            vStack.Children.Add(new TextButton("Configure Kit Midi")
            {
                HorizontalAlignment = EHorizontalAlignment.Right,
                VerticalAlignment = EVerticalAlignment.Stretch,
                ClickAction = ShowDrumMidiConfig
            });

            return vStack;
        }

        void ShowDrumMidiConfig()
        {
            ChartPlayerGame.Instance.Plugin.GameHost.IsMouseVisible = true;

            Layout.Current.ShowPopup(midiEditor);

            midiEditor.Opened();            
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

        UIElement CreateEnumOption(string property, object obj, string description)
        {
            PropertyInfo prop = obj.GetType().GetProperty(property);

            HorizontalStack hStack = new HorizontalStack()
            {
                HorizontalAlignment = EHorizontalAlignment.Stretch,
                ChildSpacing = 20
            };

            hStack.Children.Add(new TextBlock(description) { VerticalAlignment = EVerticalAlignment.Center });

            hStack.Children.Add(new UIElement { HorizontalAlignment = EHorizontalAlignment.Stretch });

            Type enumType = prop.PropertyType;

            TextButton button = new TextButton(Enum.GetName(enumType, prop.GetValue(obj)));

            List<MenuItem> items = new List<MenuItem>();

            foreach (int value in Enum.GetValues(enumType))
            {
                items.Add(new ContextMenuItem()
                {
                    Text = Enum.GetName(enumType, value),
                    AfterCloseAction = delegate
                    {
                        prop.SetValue(obj, value);
                        button.Text = Enum.GetName(enumType, prop.GetValue(obj));
                    }
                });
            }

            Menu menu = new Menu(items);

            button.ClickAction = delegate
            {
                Layout.Current.ShowPopup(menu, button.ContentBounds.Center);
            };
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

                    float pow = (int)Math.Pow(10, numDecimals);

                    value = (int)(value * pow);

                    value /= pow;

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
            string newPath = Layout.Current.GetFolder("Song Path", newSettings.SongPath);

            if (!string.IsNullOrEmpty(newPath))
            {
                newSettings.SongPath = newPath;
                songPathText.Text = newPath;

                Layout.Current.UpdateLayout();
            }
        }
    }
}
