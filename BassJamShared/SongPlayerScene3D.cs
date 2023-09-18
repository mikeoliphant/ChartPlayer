using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using PixelEngine;
using SharpDX.MediaFoundation;
using SongFormat;

namespace BassJam
{
    public class SongPlayerScene3D : Scene3D
    {
        static PixColor[] stringColors = new PixColor[] { PixColor.Red, PixColor.Yellow, new PixColor(0, .6f, 1.0f, 1.0f), PixColor.Orange, new PixColor(.1f, .8f, 0), new PixColor(.8f, 0, .8f) };

        SongPlayer player;
        float secondsLong;
        PixColor whiteHalfAlpha;
        PixColor whiteThreeQuartersAlpha;
        float currentTime;
        float timeScale = 200f;
        float positionFret = 2;
        float targetFocusFret = 2;
        float cameraDistance = 70;
        float targetCameraDistance = 70;
        int minFret = 0;
        int maxFret = 4;
        SongNote? firstNote;
        int numStrings;

        public SongPlayerScene3D(SongPlayer player, float secondsLong)
        {
            this.player = player;
            this.secondsLong = secondsLong;

            whiteHalfAlpha = PixColor.White;
            whiteHalfAlpha.A = 128;

            whiteThreeQuartersAlpha = PixColor.White;
            whiteThreeQuartersAlpha.A = 192;

            numStrings = (player.SongInstrumentPart.InstrumentType == ESongInstrumentType.BassGuitar) ? 4 : 6;
        }

        public override void Draw()
        {
            if (BassJamGame.Instance.Plugin.SongPlayer != null)
            {
                currentTime = (float)player.CurrentSecond;

                int fretDist = maxFret - minFret;

                fretDist -= 12;

                if (fretDist < 0)
                    fretDist = 0;

                targetCameraDistance = 66 + (Math.Max(fretDist, 4) * 3);

                float targetPositionFret = ((float)maxFret + (float)minFret) / 2;

                if (targetPositionFret < (targetFocusFret - 5))
                {
                    targetPositionFret = targetFocusFret - 5;
                }

                if (targetPositionFret > (targetFocusFret + 5))
                {
                    targetPositionFret = targetFocusFret + 5;
                }

                positionFret = PixUtil.Lerp(positionFret, PixUtil.Clamp(targetPositionFret, 5, 24) - 1, 0.01f);

                cameraDistance = PixUtil.Lerp(cameraDistance, targetCameraDistance, 0.01f);

                float fretOffset = (10 - positionFret) / 4.0f;

                Camera.Position = new Vector3(GetFretPosition(positionFret + fretOffset), 50, -(float)(currentTime * timeScale) + cameraDistance);
                Camera.SetLookAt(new Vector3(GetFretPosition(positionFret), 0, Camera.Position.Z - (secondsLong * timeScale) * .3f));
            }

            base.Draw();
        }

        public override void DrawQuads()
        {
            base.DrawQuads();

            FogEnabled = true;
            FogStart = 400;
            FogEnd = cameraDistance + (secondsLong * timeScale);
            FogColor = PixColor.Black;

            try
            {
                if (player != null)
                {
                    firstNote = null;

                    float startTime = currentTime;
                    float endTime = currentTime + secondsLong;

                    for (int fret = 0; fret < 24; fret++)
                    {
                        DrawFretTimeLine(fret, 0, startTime, endTime, whiteThreeQuartersAlpha);
                    }

                    minFret = 24;
                    maxFret = 0;

                    PixColor lineColor = PixColor.White;

                    foreach (SongBeat beat in player.SongStructure.Beats.Where(b => (b.TimeOffset >= startTime && b.TimeOffset <= endTime)))
                    {
                        lineColor.A = beat.IsMeasure ? (byte)128 : (byte)64;

                        DrawFretHorizontalLine(0, 23, beat.TimeOffset, 0, lineColor);
                    }

                    float startWithBuffer = startTime - 1;

                    IEnumerable<SongNote> notes = player.SongInstrumentNotes.Notes.Where(n => ((n.TimeOffset + n.TimeLength) >= startWithBuffer) && (n.TimeOffset <= endTime));

                    SongNote? lastNote = null;

                    foreach (SongNote note in notes)
                    {
                        if ((note.TimeOffset > currentTime) && (note.Fret > 0) && (!lastNote.HasValue || (lastNote.Value.Fret != note.Fret) || ((note.TimeOffset - lastNote.Value.TimeOffset) > 1)))
                        {
                            DrawVerticalText(note.Fret.ToString(), note.Fret - 0.5f, 0, note.TimeOffset, PixColor.White, 0.12f);
                        }

                        lastNote = note;
                    }

                    float startWithMinSustain = startTime - 0.15f;

                    notes = notes.Where(n => (n.TimeOffset + n.TimeLength) >= startWithMinSustain).OrderByDescending(n => n.TimeOffset);

                    foreach (SongNote note in notes)
                    {
                        if (note.TimeOffset > endTime)
                        {
                            break;
                        }

                        if (note.Techniques.HasFlag(ESongNoteTechnique.Chord))
                        {
                            SongChord chord = player.SongInstrumentNotes.Chords[note.ChordID];

                            for (int str = 0; str < chord.Fingers.Count; str++)
                            {
                                if ((chord.Fingers[str] != -1) || (chord.Frets[str] != -1))
                                {
                                    SongNote chordNote = new SongNote()
                                    {
                                        TimeOffset = note.TimeOffset,
                                        TimeLength = note.TimeLength,
                                        Fret = chord.Frets[str],
                                        String = str,
                                        Techniques = note.Techniques,
                                        HandFret = note.HandFret
                                    };

                                    DrawNote(chordNote);
                                }
                            }
                        }
                        else
                        {
                            DrawNote(note);
                        }
                    }

                    for (int str = 0; str < numStrings; str++)
                    {
                        PixColor color = stringColors[str];
                        color.A = 192;

                        DrawVerticalHorizontalLine(0, 24, startTime, GetStringHeight(str), color);
                    }

                    for (int fret = 1; fret < 24; fret++)
                    {
                        DrawFretVerticalLine(fret - 1, startTime, GetStringHeight(0), GetStringHeight(numStrings - 1), whiteHalfAlpha);

                        PixColor color = PixColor.White;

                        if (firstNote.HasValue && (fret >= firstNote.Value.HandFret) && (fret < (firstNote.Value.HandFret + 4)))
                        {
                        }
                        else
                        {
                            color.A = 64;
                        }

                        DrawVerticalText(fret.ToString(), fret - 0.5f, 1, currentTime, color, 0.08f);
                    }

                    if (firstNote.HasValue)
                    {
                        targetFocusFret = (float)firstNote.Value.HandFret + 1;
                    }
                }
            }
            catch (Exception ex)
            {
                (PixGame.Instance.CurrentGameState as PopupGameState).ShowContinuePopup("Draw error: \n\n" + ex.ToString());
            }
        }

        void DrawNote(SongNote note)
        {
            if (note.Fret == 0)
            {
                minFret = Math.Min(minFret, note.HandFret + 2);
                maxFret = Math.Max(maxFret, note.Fret);
            }
            else
            {
                minFret = Math.Min(minFret, note.Fret);
                maxFret = Math.Max(maxFret, note.HandFret + 2);
            }

            PixColor stringColor = PixColor.White;
            string imageName = null;
            string trailImageName = null;

            switch (note.String)
            {
                case 0:
                    imageName = "GuitarRed";
                    trailImageName = "NoteTrailRed";
                    break;

                case 1:
                    imageName = "GuitarYellow";
                    trailImageName = "NoteTrailYellow";
                    break;

                case 2:
                    imageName = "GuitarCyan";
                    trailImageName = "NoteTrailCyan";
                    break;

                case 3:
                    imageName = "GuitarOrange";
                    trailImageName = "NoteTrailOrange";
                    break;

                case 4:
                    imageName = "GuitarGreen";
                    trailImageName = "NoteTrailGreen";
                    break;

                case 5:
                    imageName = "GuitarPurple";
                    trailImageName = "NoteTrailPurple";
                    break;
            }

            stringColor = PixColor.Lerp(PixColor.White, stringColors[note.String], 0.25f);

            float noteHeadTime = Math.Max(note.TimeOffset, currentTime);
            float noteSustain = note.TimeLength - (noteHeadTime - note.TimeOffset);

            if (noteSustain < 0)
                noteSustain = 0;

            PixImage modifierImage = null;

            bool isMuted = false;
            bool isSlide = false;
            int slideTo = 0;

            if (note.Techniques.HasFlag(ESongNoteTechnique.HammerOn))
            {
                modifierImage = PixGame.Instance.GetImage("NoteHammerOn");
            }
            else if (note.Techniques.HasFlag(ESongNoteTechnique.PullOff))
            {
                modifierImage = PixGame.Instance.GetImage("NotePullOff");
            }
            else if (note.Techniques.HasFlag(ESongNoteTechnique.FretHandMute))
            {
                modifierImage = PixGame.Instance.GetImage("NoteMute");

                isMuted = true;
            }
            else if (note.Techniques.HasFlag(ESongNoteTechnique.PalmMute))
            {
                modifierImage = PixGame.Instance.GetImage("NoteMute");

                isMuted = true;
            }
            else if (note.Techniques.HasFlag(ESongNoteTechnique.Slide))
            {
                isSlide = true;
                slideTo = note.SlideFret;
            }

            if (isMuted)
            {
                stringColor = PixColor.Lerp(stringColor, PixColor.Black, 0.5f);
            }

            if (note.Fret == 0)   // Open string
            {
                float midAnchorFret = (float)note.HandFret + 1;

                if (note.TimeLength > 0)
                {
                    // Sustain note tail
                    DrawFlatImage(PixGame.Instance.GetImage(trailImageName), midAnchorFret, noteHeadTime, noteHeadTime + noteSustain, GetStringHeight(note.String), stringColor);
                }

                // Note head
                DrawVerticalImage(PixGame.Instance.GetImage(imageName), note.HandFret - 1, note.HandFret + 3, noteHeadTime, GetStringHeight(note.String), stringColor);

                // Note Modifier
                if (modifierImage != null)
                    DrawVerticalImage(modifierImage, midAnchorFret, noteHeadTime, GetStringHeight(note.String), PixColor.White);
            }
            else    // Fretted note
            {
                if (note.TimeLength > 0)
                {
                    // Sustain note tail

                    if (isSlide)
                    {
                        //DrawSkewedFlatImage(PixGame.Instance.GetImage(trailImageName), note.Fret - 0.5f, slideTo - 0.5f, noteHeadTime, noteHeadTime + noteSustain, GetStringHeight(note.String), stringColor);
                        DrawImageTrail(PixGame.Instance.GetImage(trailImageName), stringColor,
                            new Vector3(note.Fret - 0.5f, GetStringHeight(note.String), noteHeadTime), new Vector3(slideTo - 0.5f, GetStringHeight(note.String), noteHeadTime + noteSustain));
                    }
                    else
                    {
                        if ((note.CentsOffsets != null) && (note.CentsOffsets.Length > 0))
                        {
                            DrawBend(PixGame.Instance.GetImage(trailImageName), note.Fret - 0.5f, noteHeadTime, noteSustain, GetStringHeight(note.String), note.CentsOffsets, stringColor);
                        }
                        else
                        {
                            DrawFlatImage(PixGame.Instance.GetImage(trailImageName), note.Fret - 0.5f, noteHeadTime, noteHeadTime + noteSustain, GetStringHeight(note.String), stringColor);
                        }
                    }
                }

                // Note "shadow" on fretboard
                DrawFretHorizontalLine(note.Fret - 1, note.Fret, noteHeadTime, 0, whiteHalfAlpha);

                // Small lines for lower strings
                for (int prevString = 0; prevString < note.String; prevString++)
                {
                    DrawVerticalHorizontalLine(note.Fret - 0.6f, note.Fret - 0.4f, noteHeadTime, GetStringHeight(prevString), whiteHalfAlpha);
                }

                // Vertical line from fretboard up to note head
                DrawFretVerticalLine(note.Fret - 0.5f, noteHeadTime, 0, GetStringHeight(note.String), whiteHalfAlpha);

                float noteHeadHeight = GetStringHeight(note.String);

                if ((note.CentsOffsets != null) && (note.CentsOffsets.Length > 0))
                {
                    noteHeadHeight += GetBendOffset(note.TimeOffset, noteSustain, note.CentsOffsets);
                }

                // Note head
                DrawVerticalImage(PixGame.Instance.GetImage(imageName), note.Fret - 0.5f, noteHeadTime, noteHeadHeight, stringColor);

                // Note modifier
                if (modifierImage != null)
                    DrawVerticalImage(modifierImage, note.Fret - 0.5f, noteHeadTime, noteHeadHeight, PixColor.White);
            }

            if (note.TimeOffset > currentTime)
                firstNote = note;
        }

        float GetFretPosition(float fret)
        {
            //  s – (s / (2 ^ (n / 12)))

            return 240.0f - (240.0f / (float)(Math.Pow(2, (double)fret / 12.0)));
        }

        float GetStringHeight(float str)
        {
            return 3.0f + (str * 4.0f);
        }

        float GetCentsOffset(float cents)
        {
            return cents / 30.0f;
        }

        float GetBendOffset(float startTime, float sustain, CentsOffset[] bendOffsets)
        {
            if (startTime > currentTime)
                return 0;

            int lastCents = 0;
            float lastTime = startTime;

            foreach (CentsOffset offset in bendOffsets)
            {
                if (offset.TimeOffset > currentTime)
                {
                    float timePercent = (offset.TimeOffset - currentTime) / (offset.TimeOffset - lastTime);

                    return GetCentsOffset(PixUtil.Lerp((float)offset.Cents, (float)lastCents, timePercent));
                }

                lastCents = offset.Cents;
                lastTime = offset.TimeOffset;
            }

            return 0;
        }

        void DrawFretHorizontalLine(float startFret, float endFret, float time, float heightOffset, PixColor color)
        {
            startFret = GetFretPosition(startFret);
            endFret = GetFretPosition(endFret);
            time *= -timeScale;

            PixImage image = PixGame.Instance.GetImage("HorizontalFretLine");

            float imageScale = .08f;

            float minZ = time + ((float)image.Height * imageScale);
            float maxZ = time - ((float)image.Height * imageScale);

            DrawQuad(image, new Vector3(startFret, heightOffset, minZ), color, new Vector3(startFret, heightOffset, maxZ), color, new Vector3(endFret, heightOffset, maxZ), color, new Vector3(endFret, heightOffset, minZ), color);
        }

        void DrawVerticalHorizontalLine(float startFret, float endFret, float time, float heightOffset, PixColor color)
        {
            startFret = GetFretPosition(startFret);
            endFret = GetFretPosition(endFret);
            time *= -timeScale;

            PixImage image = PixGame.Instance.GetImage("HorizontalFretLine");

            float imageScale = .02f;

            float minY = heightOffset + ((float)image.Height * imageScale);
            float maxY = heightOffset - ((float)image.Height * imageScale);

            DrawQuad(image, new Vector3(startFret, minY, time), color, new Vector3(startFret, maxY, time), color, new Vector3(endFret, maxY, time), color, new Vector3(endFret, minY, time), color);
        }

        void DrawFretVerticalLine(float fretCenter, float time, float startHeight, float endEndHeight, PixColor color)
        {
            fretCenter = GetFretPosition(fretCenter);
            time *= -timeScale;

            PixImage image = PixGame.Instance.GetImage("VerticalFretLine");

            float imageScale = .03f;

            float minX = (float)fretCenter - ((float)image.Width * imageScale);
            float maxX = (float)fretCenter + ((float)image.Width * imageScale);

            DrawQuad(image, new Vector3(minX, startHeight, time), color, new Vector3(minX, endEndHeight, time), color, new Vector3(maxX, endEndHeight, time), color, new Vector3(maxX, startHeight, time), color);
        }

        void DrawFretTimeLine(float fretCenter, float height, float startTime, float endTime, PixColor color)
        {
            fretCenter = GetFretPosition(fretCenter);
            startTime *= -timeScale;
            endTime *= -timeScale;

            PixImage image = PixGame.Instance.GetImage("VerticalFretLine");

            float imageScale = 0.03f;

            float minX = (float)fretCenter - ((float)image.Width * imageScale);
            float maxX = (float)fretCenter + ((float)image.Width * imageScale);

            DrawQuad(image, new Vector3(minX, height, startTime), color, new Vector3(minX, height, endTime), color, new Vector3(maxX, height, endTime), color, new Vector3(maxX, height, startTime), color);
        }

        void DrawVerticalImage(PixImage image, float startFret, float endFret, float time, float heightOffset, PixColor color)
        {
            startFret = GetFretPosition(startFret);
            endFret = GetFretPosition(endFret);
            time *= -timeScale;

            float imageScale = .03f;

            float minY = heightOffset + ((float)image.Height * imageScale);
            float maxY = heightOffset - ((float)image.Height * imageScale);

            DrawQuad(image, new Vector3(startFret, minY, time), color, new Vector3(startFret, maxY, time), color, new Vector3(endFret, maxY, time), color, new Vector3(endFret, minY, time), color);
        }

        void DrawVerticalImage(PixImage image, float fretCenter, float timeCenter, float heightOffset, PixColor color)
        {
            fretCenter = GetFretPosition(fretCenter);
            timeCenter *= -timeScale;

            float imageScale = 0.08f;

            float minX = fretCenter - ((float)image.Width * imageScale);
            float maxX = fretCenter + ((float)image.Width * imageScale);

            float minY = heightOffset - ((float)image.Height * imageScale);
            float maxY = heightOffset + ((float)image.Height * imageScale);

            DrawQuad(image, new Vector3(minX, minY, timeCenter), color, new Vector3(minX, maxY, timeCenter), color, new Vector3(maxX, maxY, timeCenter), color, new Vector3(maxX, minY, timeCenter), color);
        }

        void DrawFlatImage(PixImage image, float fretCenter, float startTime, float endTime, float heightOffset, PixColor color)
        {
            fretCenter = GetFretPosition(fretCenter);
            startTime *= -timeScale;
            endTime *= -timeScale;

            float imageScale = .03f;

            float minX = fretCenter - ((float)image.Width * imageScale);
            float maxX = fretCenter + ((float)image.Width * imageScale);

            DrawQuad(image, new Vector3(minX, heightOffset, startTime), color, new Vector3(minX, heightOffset, endTime), color, new Vector3(maxX, heightOffset, endTime), color, new Vector3(maxX, heightOffset, startTime), color);
        }

        void DrawSkewedFlatImage(PixImage image, float startFret, float endFret, float startTime, float endTime, float heightOffset, PixColor color)
        {
            startFret = GetFretPosition(startFret);
            endFret = GetFretPosition(endFret);
            startTime *= -timeScale;
            endTime *= -timeScale;

            float imageScale = .03f;

            DrawQuad(image, new Vector3(startFret - ((float)image.Width * imageScale), heightOffset, startTime), color, new Vector3(endFret - ((float)image.Width * imageScale), heightOffset, endTime), color,
                new Vector3(endFret + ((float)image.Width * imageScale), heightOffset, endTime), color, new Vector3(startFret + ((float)image.Width * imageScale), heightOffset, startTime), color);
        }

        void DrawImageTrail(PixImage image, PixColor color, params Vector3[] trailPoints)
        {
            float imageScale = .03f;

            Vector3? lastPoint = null;

            foreach (Vector3 point in trailPoints)
            {
                if (lastPoint.HasValue)
                {
                    DrawQuad(image, new Vector3(GetFretPosition(lastPoint.Value.X) - ((float)image.Width * imageScale), lastPoint.Value.Y, lastPoint.Value.Z * -timeScale), color,
                        new Vector3(GetFretPosition(point.X) - ((float)image.Width * imageScale), point.Y, point.Z * -timeScale), color,
                        new Vector3(GetFretPosition(point.X) + ((float)image.Width * imageScale), point.Y, point.Z * -timeScale), color,
                        new Vector3(GetFretPosition(lastPoint.Value.X) + ((float)image.Width * imageScale), lastPoint.Value.Y, lastPoint.Value.Z * -timeScale), color);
                }

                lastPoint = point;
            }
        }

        void DrawBend(PixImage image, float fretCenter, float startTime, float sustainTime, float heightOffset, CentsOffset[] bendOffsets, PixColor color)
        {
            fretCenter = GetFretPosition(fretCenter);

            float imageScale = .03f;

            float minX = fretCenter - ((float)image.Width * imageScale);
            float maxX = fretCenter + ((float)image.Width * imageScale);

            float lastTime = startTime;
            float lastHeight = heightOffset;

            foreach (CentsOffset offset in bendOffsets)
            {
                float height = heightOffset + GetCentsOffset(offset.Cents);

                sustainTime -= (offset.TimeOffset - lastTime);

                lastTime = Math.Max(lastTime, currentTime);

                if (offset.TimeOffset > currentTime)
                {
                    DrawQuad(image, new Vector3(minX, lastHeight, lastTime * -timeScale), color,
                        new Vector3(minX, height, offset.TimeOffset * -timeScale), color,
                        new Vector3(maxX, height, offset.TimeOffset * -timeScale), color,
                        new Vector3(maxX, lastHeight, lastTime * -timeScale), color);
                }

                lastHeight = height;
                lastTime = offset.TimeOffset;
            }

            if (sustainTime > 0)
            {
                float endTime = lastTime + sustainTime;

                if (endTime > currentTime)
                {
                    lastTime = Math.Max(lastTime, currentTime);

                    DrawQuad(image, new Vector3(minX, lastHeight, lastTime * -timeScale), color,
                        new Vector3(minX, lastHeight, endTime * -timeScale), color,
                        new Vector3(maxX, lastHeight, endTime * -timeScale), color,
                        new Vector3(maxX, lastHeight, lastTime * -timeScale), color);
                }
            }
        }

        void DrawFlatText(string text, float fretCenter, float timeCenter, float heightOffset, PixColor color, float imageScale)
        {
            fretCenter = GetFretPosition(fretCenter);
            timeCenter *= -timeScale;

            PixFont font = PixGame.Instance.GetFont("LargeFont");

            float textWidth;
            float textHeight;

            font.MeasureString(text, out textWidth, out textHeight);

            textWidth *= imageScale;
            textHeight *= imageScale;

            float x = fretCenter - (textWidth / 2);

            Rectangle drawRect = Rectangle.Empty;

            for (int i = 0; i < text.Length; i++)
            {
                float z = timeCenter - (textHeight / 2);

                char c = text[i];

                PixFontGlyph glyph = font.GetGlyph(c);

                drawRect.X = glyph.X;
                drawRect.Y = glyph.Y;
                drawRect.Width = glyph.Width;
                drawRect.Height = glyph.Height;

                DrawQuad(font.FontImage, drawRect, new Vector3(x, heightOffset, z), color, new Vector3(x, heightOffset, z + ((float)glyph.Height * imageScale)), color,
                    new Vector3(x + ((float)glyph.Width * imageScale), heightOffset, z + ((float)glyph.Height * imageScale)), color,
                    new Vector3(x + ((float)glyph.Width * imageScale), heightOffset, z), color);

                x += (glyph.Width + font.Spacing) * imageScale;
            }
        }

        void DrawVerticalText(string text, float fretCenter, float verticalCenter, float timeCenter, PixColor color, float imageScale)
        {
            fretCenter = GetFretPosition(fretCenter);
            timeCenter *= -timeScale;

            PixFont font = PixGame.Instance.GetFont("LargeFont");

            float textWidth;
            float textHeight;

            font.MeasureString(text, imageScale, out textWidth, out textHeight);

            float x = fretCenter - (textWidth / 2);

            Rectangle drawRect = Rectangle.Empty;

            for (int i = 0; i < text.Length; i++)
            {
                float y = verticalCenter - (textHeight / 2);

                char c = text[i];

                PixFontGlyph glyph = font.GetGlyph(c);

                drawRect.X = glyph.X;
                drawRect.Y = glyph.Y;
                drawRect.Width = glyph.Width;
                drawRect.Height = glyph.Height;

                DrawQuad(font.FontImage, drawRect, new Vector3(x, y, timeCenter), color, new Vector3(x, y + ((float)glyph.Height * imageScale), timeCenter), color,
                    new Vector3(x + ((float)glyph.Width * imageScale), y + ((float)glyph.Height * imageScale), timeCenter), color, new Vector3(x + ((float)glyph.Width * imageScale), y, timeCenter), color);

                x += (glyph.Width + font.Spacing) * imageScale;
            }
        }
    }
}
