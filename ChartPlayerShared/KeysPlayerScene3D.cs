using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using UILayout;
using SongFormat;
using SharpDX.DirectWrite;

namespace ChartPlayer
{
    public class KeysPlayerScene3D : Scene3D
    {
        static int[] ScaleWhiteBlack = { 0, 1, 0, 1, 0, 0, 1, 0, 1, 0, 1, 0 };
        static float[] ScaleOffsets = { 0, 0.5f, 1, 1.5f, 2, 3, 3.5f, 4, 4.5f, 5, 5.5f, 6 };

        UIColor whiteHalfAlpha;
        UIColor whiteThreeQuartersAlpha;
        SongPlayer player;
        float secondsLong;
        int minKey = 48;
        int maxKey = 72;
        float timeScale = 200f;
        float currentTime;
        float targetCameraDistance = 64;
        float cameraDistance = 70;
        float positionKey;
        float startTime;
        float endTime;

        public KeysPlayerScene3D(SongPlayer player, float secondsLong)
        {
            this.player = player;
            this.secondsLong = secondsLong;

            whiteHalfAlpha = UIColor.White;
            whiteHalfAlpha.A = 128;

            whiteThreeQuartersAlpha = UIColor.White;
            whiteThreeQuartersAlpha.A = 192;

            positionKey = ((float)maxKey + (float)minKey) / 2;
        }

        public override void Draw()
        {
            if (ChartPlayerGame.Instance.Plugin.SongPlayer != null)
            {
                currentTime = (float)player.CurrentSecond;

                int keyDist = maxKey - minKey;

                keyDist -= 12;

                if (keyDist < 0)
                    keyDist = 0;

                targetCameraDistance = 60 + (Math.Max(keyDist, 4) * 3);

                float targetPositionKey = ((float)maxKey + (float)minKey) / 2;

                positionKey = MathUtil.Lerp(positionKey, targetPositionKey, 0.01f);

                cameraDistance = MathUtil.Lerp(cameraDistance, targetCameraDistance, 0.01f);

                Camera.Position = new Vector3(GetKeyPosition(positionKey), 50, -(float)(currentTime * timeScale) + cameraDistance);
                Camera.SetLookAt(new Vector3(GetKeyPosition(positionKey), 0, Camera.Position.Z - (secondsLong * timeScale) * .3f));
            }

            base.Draw();
        }

        public override void DrawQuads()
        {
            base.DrawQuads();

            FogEnabled = true;
            FogStart = 400;
            FogEnd = cameraDistance + (secondsLong * timeScale);
            FogColor = UIColor.Black;

            try
            {
                if (player != null)
                {
                    startTime = currentTime;
                    endTime = currentTime + secondsLong;

                    for (int key = minKey; key <= (maxKey + 2); key++)
                    {
                        if (ScaleWhiteBlack[(key - minKey) % 12] == 0)
                        {
                            DrawKeyTimeLine(key, 0, startTime, endTime, whiteHalfAlpha);
                        }
                    }

                    UIColor lineColor = UIColor.White;

                    foreach (SongBeat beat in player.SongStructure.Beats.Where(b => (b.TimeOffset >= startTime && b.TimeOffset <= endTime)))
                    {
                        lineColor.A = beat.IsMeasure ? (byte)128 : (byte)64;

                        DrawKeyHorizontalLine(minKey, maxKey + 2, beat.TimeOffset, 0, lineColor, .08f);
                    }

                    float startWithMinSustain = startTime - 0.15f;

                    var notes = player.SongKeyboardNotes.Notes.Where(n => (n.TimeOffset + n.TimeLength) >= startWithMinSustain).OrderByDescending(n => n.TimeOffset);

                    // Draw the notes
                    foreach (SongKeyboardNote note in notes)
                    {
                        if ((note.Note < minKey) || (note.Note > maxKey))
                        {

                        }
                        else
                        {
                            bool isWhite = (ScaleWhiteBlack[(note.Note - minKey) % 12] == 0);

                            float startTime = Math.Max(note.TimeOffset, currentTime);

                            DrawFlatImage(Layout.Current.GetImage(isWhite ? "NoteTrailWhite" : "NoteTrailBlack"), (float)note.Note + 0.5f, startTime, note.TimeOffset + note.TimeLength, 0, UIColor.White, 0.06f);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Layout.Current.ShowContinuePopup("Draw error: \n\n" + ex.ToString());
            }
        }

        float GetKeyPosition(float key)
        {
            int intKey = (int)key;

            if (key == intKey)
            {
                int octave = ((intKey - minKey) / 12);

                return (ScaleOffsets[(intKey - minKey) % 12] + (octave * 7)) * 8;
            }

            float pos = GetKeyPosition(intKey);
            float frac = key - intKey;

            return MathUtil.Lerp(pos, pos + 8, frac);
        }

        void DrawKeyTimeLine(float keyCenter, float height, float startTime, float endTime, UIColor color)
        {
            keyCenter = GetKeyPosition(keyCenter);
            startTime *= -timeScale;
            endTime *= -timeScale;

            UIImage image = Layout.Current.GetImage("VerticalFretLine");

            float imageScale = 0.03f;

            float minX = (float)keyCenter - ((float)image.Width * imageScale);
            float maxX = (float)keyCenter + ((float)image.Width * imageScale);

            DrawQuad(image, new Vector3(minX, height, startTime), color, new Vector3(minX, height, endTime), color, new Vector3(maxX, height, endTime), color, new Vector3(maxX, height, startTime), color);
        }

        void DrawKeyHorizontalLine(float startKey, float endKey, float time, float heightOffset, UIColor color, float imageScale)
        {
            startKey = GetKeyPosition(startKey);
            endKey = GetKeyPosition(endKey);
            time *= -timeScale;

            UIImage image = Layout.Current.GetImage("HorizontalFretLine");

            float minZ = time + ((float)image.Height * imageScale);
            float maxZ = time - ((float)image.Height * imageScale);

            DrawQuad(image, new Vector3(startKey, heightOffset, minZ), color, new Vector3(startKey, heightOffset, maxZ), color, new Vector3(endKey, heightOffset, maxZ), color, new Vector3(endKey, heightOffset, minZ), color);
        }

        void DrawFlatImage(UIImage image, float keyCenter, float startTime, float endTime, float heightOffset, UIColor color, float imageScale)
        {
            keyCenter = GetKeyPosition(keyCenter);
            startTime *= -timeScale;
            endTime *= -timeScale;

            float minX = keyCenter - ((float)image.Width * imageScale);
            float maxX = keyCenter + ((float)image.Width * imageScale);

            DrawQuad(image, new Vector3(minX, heightOffset, startTime), color, new Vector3(minX, heightOffset, endTime), color, new Vector3(maxX, heightOffset, endTime), color, new Vector3(maxX, heightOffset, startTime), color);
        }
    }
}
