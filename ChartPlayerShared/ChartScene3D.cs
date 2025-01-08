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
    public interface IMidiHandler
    {
        void HandleNoteOn(int channel, int noteNumber, float velocity, int sampleOffset);
        void HandlePolyPressure(int channel, int noteNumber, float pressure, int sampleOffset);
    }

    public class ChartScene3D : Scene3D
    {
        public float NoteDisplaySeconds { get; set; } = 3;
        public float NoteDisplayDistance { get; set; } = 600;
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
        public float CurrentTimeOffset { get; set; } = 0;

        protected SongPlayer player;
        protected float timeScale;
        protected float currentTime;
        protected float startTime;
        protected float endTime;
        protected float highwayStartX;
        protected float highwayEndX;
        protected UIColor whiteHalfAlpha;
        protected UIColor whiteThreeQuartersAlpha;
        protected bool lefyMode = false;
        int startBeatPosition = 0;

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

        public int GetStartNote<T>(float timeOffset, float minLength, int startNotePosition, IList<T> notes) where T : ISongEvent
        {
            startNotePosition = MathUtil.Clamp(startNotePosition, 0, notes.Count - 1);

            if (startNotePosition < 0)
                startNotePosition = 0;

            // Move backward until we find a note that is not visible (or we hit the start)
            while (startNotePosition > 0)
            {
                ISongEvent note = notes[startNotePosition];

                float endTime = Math.Max(note.EndTime, note.TimeOffset + minLength);

                if (endTime < timeOffset)
                    break;

                startNotePosition--;
            }

            // Now move forward until we find a note that is visible (or we hit the end)
            while (startNotePosition < notes.Count)
            {
                ISongEvent note = notes[startNotePosition];

                float endTime = Math.Max(note.EndTime, note.TimeOffset + minLength);

                if (endTime > timeOffset)
                    break;

                startNotePosition++;
            }

            return startNotePosition;
        }

        public int GetEndNote<T>(int startPosition, float endTime, IList<T> notes) where T : ISongEvent
        {
            while (startPosition < notes.Count)
            {
                ISongEvent note = notes[startPosition];

                if (note.TimeOffset > endTime)
                    break;

                startPosition++;
            }

            if (startPosition == notes.Count)
                startPosition--;

            return startPosition;
        }

        public override void Draw()
        {
            base.Draw();

            timeScale = NoteDisplayDistance / NoteDisplaySeconds;

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

            var allBeats = player.SongStructure.Beats;

            startBeatPosition = GetStartNote<SongBeat>(currentTime - CurrentTimeOffset, 0, startBeatPosition, allBeats);

            int pos = 0;

            // Draw hand position areas on timeline
            for (pos = startBeatPosition; pos < allBeats.Count; pos++)
            {
                SongBeat beat = allBeats[pos];

                if (beat.TimeOffset > endTime)
                    break;

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
