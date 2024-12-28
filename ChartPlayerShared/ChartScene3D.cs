using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Xna.Framework;
using SongFormat;
using UILayout;

namespace ChartPlayer
{
    public class ChartScene3D : Scene3D
    {
        public float NoteDisplaySeconds { get; set; } = 3;
        public int NumNotesDetected { get; protected set; } = 0;
        public int NumNotesTotal { get; protected set; } = 0;
        public float CurrentBPM { get; protected set; } = 0;
        public bool LeftyMode
        {
            get => lefyMode;
            set
            {
                lefyMode = value;
                Camera.MirrorLeftRight = lefyMode;
            }
        }

        protected SongPlayer player;
        protected float timeScale = 200f;
        protected float currentTime;
        protected float startTime;
        protected float endTime;
        protected float highwayStartX;
        protected float highwayEndX;
        protected UIColor whiteHalfAlpha;
        protected UIColor whiteThreeQuartersAlpha;
        protected bool lefyMode = false;


        public ChartScene3D(SongPlayer player)
        {
            this.player = player;

            whiteHalfAlpha = UIColor.White;
            whiteHalfAlpha.A = 128;

            whiteThreeQuartersAlpha = UIColor.White;
            whiteThreeQuartersAlpha.A = 192;
        }

        public virtual void ResetScore()
        {
            NumNotesDetected = 0;
            NumNotesTotal = 0;
        }

        public override void Draw()
        {
            base.Draw();

            currentTime = (float)player.CurrentSecond;

            startTime = currentTime;
            endTime = currentTime + NoteDisplaySeconds;
        }

        public override void DrawQuads()
        {
            base.DrawQuads();

            DrawBeats();
        }

        public virtual void DrawBeats()
        {
            CurrentBPM = 0;

            float lastBeatTime = 0;

            foreach (SongBeat beat in player.SongStructure.Beats.Where(b => (b.TimeOffset >= startTime && b.TimeOffset <= endTime)))
            {
                DrawBeat(beat.TimeOffset, beat.IsMeasure);

                if (lastBeatTime == 0)
                {
                    lastBeatTime = beat.TimeOffset;
                }
                else if (CurrentBPM == 0)
                {
                    float delta = beat.TimeOffset - lastBeatTime;

                    CurrentBPM = (float)((1.0 / delta) * 60);
                }
            }
        }

        public virtual void DrawBeat(float timeOffset, bool isMeasure)
        {
            UIColor lineColor = UIColor.White;
            lineColor.A = isMeasure ? (byte)128 : (byte)64;

            DrawHorizontalLine(highwayStartX, highwayEndX, timeOffset, 0, lineColor, isMeasure ? .12f : .08f);

        }

        void DrawHorizontalLine(float startX, float endX, float time, float heightOffset, UIColor color, float imageScale)
        {
            time *= -timeScale;

            UIImage image = Layout.Current.GetImage("HorizontalFretLine");

            float minZ = time + ((float)image.Height * imageScale);
            float maxZ = time - ((float)image.Height * imageScale);

            DrawQuad(image, new Vector3(startX, heightOffset, minZ), color, new Vector3(startX, heightOffset, maxZ), color, new Vector3(endX, heightOffset, maxZ), color, new Vector3(endX, heightOffset, minZ), color);
        }
    }
}
