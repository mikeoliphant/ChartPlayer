using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PixelEngine;

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
            : base(PopupGameState.DefaultPopupNinePatch)
        {
            oldSettings = settings;

            CopySettings(oldSettings, newSettings);

            VerticalStack vStack = new VerticalStack() { ChildSpacing = PixUI.DefaultScale * 5, VerticalAlignment = EVerticalAlignment.Stretch };
            SetContents(vStack);

            HorizontalStack invertStack = new HorizontalStack()
            {
                ChildSpacing = PixUI.DefaultScale * 5
            };
            vStack.Children.Add(invertStack);

            invertStack.Children.Add(new TextBlock("String Orientation:"));

            TextToggleButton invertStringsButton = new TextToggleButton("InvertStrings", "Low On Top", "Low On Bottom");
            invertStack.Children.Add(invertStringsButton);

            invertStringsButton.SetPressed(newSettings.InvertStrings);
            invertStringsButton.Update(0);

            HorizontalStack retuneStack = new HorizontalStack()
            {
                ChildSpacing = PixUI.DefaultScale * 5
            };
            vStack.Children.Add(retuneStack);

            retuneStack.Children.Add(new TextBlock("Re-tune to E Standard:"));

            TextToggleButton retuneButton = new TextToggleButton("DoRetune", "Yes", "No");
            retuneStack.Children.Add(retuneButton);

            retuneButton.SetPressed(newSettings.RetuneToEStandard);
            retuneButton.Update(0);

            AddInput(new DialogInput { Text = "Apply", ButtonName = "MenuSelect", Action = Apply, CloseOnInput = true });
            AddInput(new DialogInput { Text = "Cancel", ButtonName = "MenuBack", CloseOnInput = true });
        }

        void CopySettings(SongPlayerSettings fromSettings, SongPlayerSettings toSettings)
        {
            foreach (PropertyInfo property in typeof(SongPlayerSettings).GetProperties().Where(p => p.CanWrite))
            {
                property.SetValue(toSettings, property.GetValue(fromSettings, null), null);
            }
        }

        public override void HandleInput(PixInputManager inputManager)
        {
            base.HandleInput(inputManager);

            if (inputManager.WasClicked("InvertStrings", this))
            {
                newSettings.InvertStrings = !newSettings.InvertStrings;

                PixGame.Instance.UserInterface.NeedLayoutUpdate = true;
            }

            if (inputManager.WasClicked("DoRetune", this))
            {
                newSettings.RetuneToEStandard = !newSettings.RetuneToEStandard;

                PixGame.Instance.UserInterface.NeedLayoutUpdate = true;
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
