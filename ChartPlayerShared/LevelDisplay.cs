using System;
using System.Drawing;
using UILayout;

namespace ChartPlayer
{
    public class AudioLevelDisplay : Dock
    {
        public double WarnLevel { get; set; }

        ImageElement activeLevelImage;
        double lastValue = 0;
        double clip = 0;

        public AudioLevelDisplay()
        {
            WarnLevel = 0.8f;
            BackgroundColor = UIColor.Black;
            HorizontalAlignment = EHorizontalAlignment.Stretch;
            VerticalAlignment = EVerticalAlignment.Stretch;

            Children.Add(new ImageElement("LevelDisplay")
            {
                Color = new UIColor(20, 20, 20),
            });

            activeLevelImage = new ImageElement("LevelDisplay")
            {
                HorizontalAlignment = EHorizontalAlignment.Stretch,
                VerticalAlignment = EVerticalAlignment.Bottom,
                Color = UIColor.Green,
            };
            activeLevelImage.Visible = false;
            Children.Add(activeLevelImage);
        }

        public void SetValue(double value)
        {
            if (value < 0.01)
                value = 0;

            if (clip > 0)
            {
                clip -= 0.1;

                if (clip <= 0)
                    activeLevelImage.Color = UIColor.Green;
            }

            if (value >= 1.0)
            {
                clip = 1;
                activeLevelImage.Color = UIColor.Red;
            }
            else if (value >= WarnLevel)
            {
                clip = 1;
                activeLevelImage.Color = UIColor.Orange;
            }

            if (value != lastValue)
            {
                lastValue = value;

                double logLevel = Math.Min(Math.Log10((value * 9.0) + 1.0), 1.0);

                int height = (int)((double)activeLevelImage.Image.Height * logLevel);

                if (height > activeLevelImage.Image.Height)
                    height = activeLevelImage.Image.Height;

                activeLevelImage.SourceRectangle = new Rectangle(0, activeLevelImage.Image.Height - height, activeLevelImage.Image.Width, height);
                activeLevelImage.DesiredHeight = ContentBounds.Height * (float)logLevel;
                activeLevelImage.Visible = (logLevel > 0);

                UpdateContentLayout();
            }
        }
    }
}
