using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UILayout;

namespace ChartPlayer
{
    public class HelpDialog : InputDialog
    {
        public HelpDialog(UIElement helpElement)
            : base(Layout.Current.GetImage("PopupBackground"))
        {
            VerticalStack contents = new VerticalStack()
            {
                ChildSpacing = 20
            };

            contents.Children.Add(new TextBlock("ChartPlayer v0.1.3\nCopyright (c) 2024 Mike Oliphant"));

            contents.Children.Add(helpElement);

            SetContents(contents);

            AddInput(new DialogInput()
            {
                Text = "Close",
                CloseOnInput = true
            });

            AddInput(new DialogInput()
            {
                Text = "View Github Project",
                CloseOnInput = false,
                Action = () => { Process.Start(new ProcessStartInfo("https://github.com/mikeoliphant/ChartPlayer") { UseShellExecute = true }); }
            });
        }
    }
}
