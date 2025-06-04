using System;
using System.Linq;
using UILayout;
using SongFormat;

namespace ChartPlayer
{
    public class VocalDisplay : UIElement
    {
        public SongPlayer SongPlayer { get; set; }

        UIFont font;
        float spaceWidth;

        public VocalDisplay()
        {
            font = ChartPlayerGame.Instance.GetFont("LargeFont");

            float height;

            font.MeasureString(" ", out spaceWidth, out height);
        }

        protected override void GetContentSize(out float width, out float height)
        {
            // Dummy values just so that the UI doesn't think we're empty
            width = height = 10;
        }

        protected override void DrawContents()
        {
            if (SongPlayer != null)
            {
                float startTime = SongPlayer.CurrentSecond - 1;
                float endTime = SongPlayer.CurrentSecond + 2;

                float width;
                float height;

                float xOffset = ContentBounds.X;
                float yOffset = ContentBounds.Y;

                GraphicsContext2D context = Layout.Current.GraphicsContext;

                int vocalPos = 0;

                for (; vocalPos < SongPlayer.SongVocals.Count; vocalPos++)
                {
                    if (SongPlayer.SongVocals[vocalPos].TimeOffset > startTime)
                        break;
                }

                if (vocalPos > 0)
                    vocalPos--;

                if (vocalPos == SongPlayer.SongVocals.Count)
                    return;

                for (; vocalPos >= 0; vocalPos--)
                {
                    if (SongPlayer.SongVocals[vocalPos].Vocal.EndsWith('\n') || ((startTime - SongPlayer.SongVocals[vocalPos].TimeOffset) > 20))
                    {
                        vocalPos++;
                        break;
                    }
                }

                if (vocalPos < 0)
                    vocalPos = 0;

                for (; vocalPos < SongPlayer.SongVocals.Count; vocalPos++)
                {
                    SongVocal vocal = SongPlayer.SongVocals[vocalPos];

                    if (vocal.TimeOffset > endTime)
                        break;

                    font.MeasureString(vocal.Vocal, out width, out height);

                    // Force a wrap if we're too wide for the content bounds
                    if (xOffset + width > ContentBounds.Right)
                    {
                        xOffset = ContentBounds.X;
                        yOffset += font.TextHeight;
                    }

                    if ((vocal.TimeOffset < SongPlayer.CurrentSecond) && ((SongPlayer.CurrentSecond - vocal.TimeOffset)) < 0.5f)
                    {
                        context.DrawText(vocal.Vocal, font, xOffset, yOffset, UIColor.Yellow);
                    }
                    else
                    {
                        context.DrawText(vocal.Vocal, font, xOffset, yOffset, UIColor.White);
                    }

                    xOffset += width;

                    if (vocal.Vocal.EndsWith('\n'))
                    {
                        xOffset = ContentBounds.X;
                        yOffset += font.TextHeight;
                    }
                    else
                    {
                        if (!vocal.Vocal.EndsWith('-'))
                        {
                            xOffset += spaceWidth;
                        }
                    }
                }
            }

            base.DrawContents();
        }
    }
}
