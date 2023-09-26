using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UILayout;

namespace BassJam
{
    public class SongPlayerSettings
    {
        public bool InvertStrings { get; set; } = false;
        public bool RetuneToEStandard { get; set; } = true;
    }

    public class SongPlayerSettingsInterface : InputDialog
    {
        public Action ApplyAction { get; set; }

        SongPlayerSettings oldSettings;
        SongPlayerSettings newSettings = new SongPlayerSettings();

        public SongPlayerSettingsInterface(SongPlayerSettings settings)
            : base(Layout.DefaultOutlineNinePatch)
        {
            oldSettings = settings;

            CopySettings(oldSettings, newSettings);

            VerticalStack vStack = new VerticalStack() { ChildSpacing = 10, VerticalAlignment = EVerticalAlignment.Stretch };
            SetContents(vStack);

            HorizontalStack invertStack = new HorizontalStack()
            {
                HorizontalAlignment = EHorizontalAlignment.Stretch,
                ChildSpacing = 20
            };
            vStack.Children.Add(invertStack);

            invertStack.Children.Add(new TextBlock("String Orientation:"){ VerticalAlignment = EVerticalAlignment.Center });

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

            TextToggleButton retuneButton = new TextToggleButton("Yes", "No")
            {                
                HorizontalAlignment = EHorizontalAlignment.Stretch,
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
    }
}
