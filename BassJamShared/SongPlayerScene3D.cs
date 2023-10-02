using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic.Logging;
using Microsoft.Xna.Framework;
using UILayout;
using SongFormat;

namespace BassJam
{
    public class SongPlayerScene3D : Scene3D
    {
        static UIColor[] stringColors = new UIColor[] { UIColor.Red, UIColor.Yellow, new UIColor(0, .6f, 1.0f, 1.0f), UIColor.Orange, new UIColor(.1f, .8f, 0), new UIColor(.8f, 0, .8f) };

        SongPlayer player;
        float secondsLong;
        UIColor whiteHalfAlpha;
        UIColor whiteThreeQuartersAlpha;
        float currentTime;
        float timeScale = 200f;
        float positionFret = 2;
        float targetFocusFret = 2;
        float cameraDistance = 70;
        float targetCameraDistance = 64;
        int minFret = 0;
        int maxFret = 4;
        SongNote? firstNote;
        int numStrings;
        bool isDetected = false;

        Dictionary<float, bool> nonReapeatDict = new Dictionary<float, bool>();

        public SongPlayerScene3D(SongPlayer player, float secondsLong)
        {
            this.player = player;
            this.secondsLong = secondsLong;

            whiteHalfAlpha = UIColor.White;
            whiteHalfAlpha.A = 128;

            whiteThreeQuartersAlpha = UIColor.White;
            whiteThreeQuartersAlpha.A = 192;

            numStrings = (player.SongInstrumentPart.InstrumentType == ESongInstrumentType.BassGuitar) ? 4 : 6;

            // Do a pre-pass on the notes to find spot where we want to add note numbers on the fretboard or re-show the full chord
            SongNote? lastNote = null;
            float lastTime = currentTime;

            UIColor handPositionColor = UIColor.White;
            handPositionColor.A = 32;

            foreach (SongNote note in player.SongInstrumentNotes.Notes)
            {
                if (!lastNote.HasValue)
                {
                    nonReapeatDict[note.TimeOffset] = true;
                }
                else
                {
                    if (note.HandFret != lastNote.Value.HandFret)
                    {
                        nonReapeatDict[note.TimeOffset] = true;
                    }
                    else
                    {
                        if (note.Techniques.HasFlag(ESongNoteTechnique.Chord))
                        {
                            if ((lastNote.Value.ChordID != note.ChordID) || ((note.TimeOffset - lastNote.Value.TimeOffset) > 1))
                            {
                                nonReapeatDict[note.TimeOffset] = true;
                            }
                        }
                        else if ((note.Fret > 0) && ((lastNote.Value.Fret != note.Fret) || ((note.TimeOffset - lastNote.Value.TimeOffset) > 1)))
                        {
                            nonReapeatDict[note.TimeOffset] = true;
                        }
                    }
                }

                lastNote = note;
            }
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

                targetCameraDistance = 60 + (Math.Max(fretDist, 4) * 3);

                float targetPositionFret = ((float)maxFret + (float)minFret) / 2;

                if (targetPositionFret < (targetFocusFret - 5))
                {
                    targetPositionFret = targetFocusFret - 5;
                }

                if (targetPositionFret > (targetFocusFret + 5))
                {
                    targetPositionFret = targetFocusFret + 5;
                }

                positionFret = MathUtil.Lerp(positionFret, MathUtil.Clamp(targetPositionFret, 4, 24) - 1, 0.01f);

                cameraDistance = MathUtil.Lerp(cameraDistance, targetCameraDistance, 0.01f);

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
            FogColor = UIColor.Black;

            try
            {
                if (player != null)
                {
                    firstNote = null;

                    float startTime = currentTime;
                    float endTime = currentTime + secondsLong;

                    for (int fret = 0; fret < 24; fret++)
                    {
                        DrawFretTimeLine(fret, 0, startTime, endTime, whiteHalfAlpha);
                    }

                    minFret = 24;
                    maxFret = 0;

                    UIColor lineColor = UIColor.White;

                    foreach (SongBeat beat in player.SongStructure.Beats.Where(b => (b.TimeOffset >= startTime && b.TimeOffset <= endTime)))
                    {
                        lineColor.A = beat.IsMeasure ? (byte)128 : (byte)64;

                        DrawFretHorizontalLine(0, 23, beat.TimeOffset, 0, lineColor, .08f);
                    }

                    float startWithBuffer = startTime - 1;

                    IEnumerable<SongNote> notes = player.SongInstrumentNotes.Notes.Where(n => ((n.TimeOffset + n.TimeLength) >= startWithBuffer) && (n.TimeOffset <= endTime));

                    int lastHandFret = -1;
                    float lastTime = currentTime;

                    UIColor handPositionColor = UIColor.White;
                    handPositionColor.A = 32;

                    foreach (SongNote note in notes)
                    {
                        if ((note.TimeOffset > currentTime) && ((lastHandFret != -1) && (lastHandFret != note.HandFret)))
                        {
                            DrawFlatImage(Layout.Current.GetImage("SingleWhitePixel"), lastHandFret - 1, lastHandFret + 3, lastTime, note.TimeOffset, 0, handPositionColor);

                            lastTime = note.TimeOffset;
                        }

                        lastHandFret = note.HandFret;
                    }

                    if (lastHandFret != -1)
                    {
                        DrawFlatImage(Layout.Current.GetImage("SingleWhitePixel"), lastHandFret - 1, lastHandFret + 3, lastTime, endTime, 0, handPositionColor);
                    }

                    float startWithMinSustain = startTime - 0.15f;

                    notes = notes.Where(n => (n.TimeOffset + n.TimeLength) >= startWithMinSustain).OrderByDescending(n => n.TimeOffset).ThenBy(n => GetStringOffset(n.String));

                    foreach (SongNote note in notes)
                    {
                        if (note.TimeOffset > endTime)
                        {
                            break;
                        }

                        isDetected = false;

                        if (note.TimeOffset <= currentTime)
                        {
                            isDetected = NoteDetect(note);
                        }

                        if (note.Techniques.HasFlag(ESongNoteTechnique.Chord))
                        {
                            SongChord chord = player.SongInstrumentNotes.Chords[note.ChordID];

                            UIColor color = UIColor.White;
                            color.A = 32;

                            int minFret = 24;
                            int maxFret = 0;
                            int minString = numStrings - 1;
                            int maxString = 0;

                            for (int str = 0; str < chord.Fingers.Count; str++)
                            {
                                if ((chord.Fingers[str] != -1) || (chord.Frets[str] != -1))
                                {
                                    if (chord.Frets[str] > 0)
                                    {
                                        minFret = Math.Min(minFret, chord.Frets[str]);
                                        maxFret = Math.Max(maxFret, chord.Frets[str]);
                                    }

                                    minString = Math.Min(minString, GetStringOffset(str));
                                    maxString = Math.Max(maxString, GetStringOffset(str));

                                    SongNote chordNote = new SongNote()
                                    {
                                        TimeOffset = note.TimeOffset,
                                        TimeLength = note.TimeLength,
                                        Fret = chord.Frets[str],
                                        String = str,
                                        Techniques = note.Techniques,
                                        HandFret = note.HandFret
                                    };

                                    if (nonReapeatDict.ContainsKey(note.TimeOffset))
                                    {
                                        if ((note.TimeOffset > currentTime) && (chord.Frets[str] > 0))
                                            DrawVerticalText(chord.Frets[str].ToString(), chord.Frets[str] - 0.5f, 0, note.TimeOffset, UIColor.White, 0.12f);

                                        DrawNote(chordNote);
                                    }
                                }
                            }

                            if ((note.TimeOffset > currentTime) && (maxFret > minFret))
                            {
                                DrawVerticalImage(Layout.Current.GetImage("SingleWhitePixel"), minFret - 1, maxFret, note.TimeOffset, 0, GetStringHeight(maxString + 1), color);

                                if (nonReapeatDict.ContainsKey(note.TimeOffset) && !String.IsNullOrEmpty(chord.Name))
                                {
                                    DrawVerticalText(chord.Name, minFret - 1.25f, GetStringHeight(maxString), note.TimeOffset, UIColor.White, 0.10f);
                                }
                            }
                        }
                        else
                        {
                            if ((note.TimeOffset > currentTime) && (note.Fret > 0) && nonReapeatDict.ContainsKey(note.TimeOffset))
                                DrawVerticalText(note.Fret.ToString(), note.Fret - 0.5f, 0, note.TimeOffset, UIColor.White, 0.12f);

                            DrawNote(note);
                        }
                    }

                    for (int str = 0; str < numStrings; str++)
                    {
                        UIColor color = stringColors[str];
                        color.A = 192;

                        DrawFretHorizontalLine(0, 24, startTime, GetStringHeight(GetStringOffset(str)), color, .04f);
                    }

                    for (int fret = 1; fret < 24; fret++)
                    {
                        DrawFretVerticalLine(fret - 1, startTime, GetStringHeight(0), GetStringHeight(numStrings - 1), whiteHalfAlpha);

                        UIColor color = UIColor.White;

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
                Layout.Current.ShowContinuePopup("Draw error: \n\n" + ex.ToString());
            }
        }

        void DrawNote(SongNote note)
        {
            if (isDetected)
            {

            }

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

            UIColor stringColor = UIColor.White;
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

            if (!isDetected)
            {
                stringColor = UIColor.Lerp(UIColor.White, stringColors[note.String], 0.25f);
                stringColor = UIColor.Lerp(stringColor, UIColor.Black, 0.25f);
            }

            float noteHeadTime = Math.Max(note.TimeOffset, currentTime);
            float noteSustain = note.TimeLength - (noteHeadTime - note.TimeOffset);

            if (noteSustain < 0)
                noteSustain = 0;

            UIImage modifierImage = null;

            bool isMuted = false;
            bool isSlide = false;
            int slideTo = 0;

            if (note.Techniques.HasFlag(ESongNoteTechnique.HammerOn))
            {
                modifierImage = Layout.Current.GetImage("NoteHammerOn");
            }
            else if (note.Techniques.HasFlag(ESongNoteTechnique.PullOff))
            {
                modifierImage = Layout.Current.GetImage("NotePullOff");
            }
            else if (note.Techniques.HasFlag(ESongNoteTechnique.FretHandMute))
            {
                modifierImage = Layout.Current.GetImage("NoteMute");

                isMuted = true;
            }
            else if (note.Techniques.HasFlag(ESongNoteTechnique.PalmMute))
            {
                modifierImage = Layout.Current.GetImage("NoteMute");

                isMuted = true;
            }
            else if (note.Techniques.HasFlag(ESongNoteTechnique.Slide))
            {
                isSlide = true;
                slideTo = note.SlideFret;
            }

            if (isMuted)
            {
                stringColor = UIColor.Lerp(stringColor, UIColor.Black, 0.5f);
            }

            int stringOffset = GetStringOffset(note.String);

            if (note.Fret == 0)   // Open string
            {
                float midAnchorFret = (float)note.HandFret + 1;

                if (note.TimeLength > 0)
                {
                    // Sustain note tail
                    DrawFlatImage(Layout.Current.GetImage(trailImageName), midAnchorFret, noteHeadTime, noteHeadTime + noteSustain, GetStringHeight(stringOffset), stringColor, .05f);
                }

                // Note head
                if (isDetected)
                    DrawVerticalImage(Layout.Current.GetImage("GuitarDetected"), note.HandFret - 1, note.HandFret + 3, noteHeadTime, GetStringHeight(stringOffset), stringColor, 0.05f);

                DrawVerticalImage(Layout.Current.GetImage(imageName), note.HandFret - 1, note.HandFret + 3, noteHeadTime, GetStringHeight(stringOffset), stringColor, 0.03f);

                // Note Modifier
                if (modifierImage != null)
                    DrawVerticalImage(modifierImage, midAnchorFret, noteHeadTime, GetStringHeight(stringOffset), UIColor.White, 0.08f);
            }
            else    // Fretted note
            {
                if (note.TimeLength > 0)
                {
                    // Sustain note tail

                    if (isSlide)
                    {
                        //DrawSkewedFlatImage(PixGame.Instance.GetImage(trailImageName), note.Fret - 0.5f, slideTo - 0.5f, noteHeadTime, noteHeadTime + noteSustain, GetStringHeight(note.String), stringColor);

                        DrawImageTrail(Layout.Current.GetImage(trailImageName), stringColor, 0.03f,
                            new Vector3(note.Fret - 0.5f, GetStringHeight(stringOffset), noteHeadTime), new Vector3(slideTo - 0.5f, GetStringHeight(stringOffset), noteHeadTime + noteSustain));
                    }
                    else
                    {
                        if ((note.CentsOffsets != null) && (note.CentsOffsets.Length > 0))
                        {
                            DrawBend(Layout.Current.GetImage(trailImageName), note.Fret - 0.5f, noteHeadTime, noteSustain, stringOffset, note.CentsOffsets, stringColor);
                        }
                        else
                        {
                            if (note.Techniques.HasFlag(ESongNoteTechnique.Vibrato))
                            {
                                DrawVibrato(Layout.Current.GetImage(trailImageName), note.Fret - 0.5f, note.TimeOffset, note.TimeOffset + note.TimeLength, GetStringHeight(stringOffset), stringColor);
                            }
                            else
                            {
                                DrawFlatImage(Layout.Current.GetImage(trailImageName), note.Fret - 0.5f, noteHeadTime, noteHeadTime + noteSustain, GetStringHeight(stringOffset), stringColor, .03f);
                            }
                        }
                    }
                }

                // Note "shadow" on fretboard
                DrawFretHorizontalLine(note.Fret - 1, note.Fret, noteHeadTime, 0, whiteHalfAlpha, 0.08f);

                // Small lines for lower strings
                for (int prevString = 0; prevString < stringOffset; prevString++)
                {
                    DrawFretHorizontalLine(note.Fret - 0.6f, note.Fret - 0.4f, noteHeadTime, GetStringHeight(prevString), whiteHalfAlpha, .04f);
                }

                // Vertical line from fretboard up to note head
                DrawFretVerticalLine(note.Fret - 0.5f, noteHeadTime, 0, GetStringHeight(stringOffset), whiteHalfAlpha);

                float noteHeadHeight = GetStringHeight(stringOffset);

                if ((note.CentsOffsets != null) && (note.CentsOffsets.Length > 0))
                {
                    float bendOffset = GetBendOffset(note.TimeOffset, note.TimeLength, note.String, note.CentsOffsets);

                    if (BassJamGame.Instance.Plugin.BassJamSaveState.SongPlayerSettings.InvertStrings)
                        noteHeadHeight -= bendOffset;
                    else
                        noteHeadHeight += bendOffset;

                }

                // Note head
                if (!note.Techniques.HasFlag(ESongNoteTechnique.Continued) || (note.TimeOffset < currentTime))
                {
                    if (isSlide && (note.TimeOffset < currentTime))
                    {
                        float slideFret = MathUtil.Lerp(note.Fret, note.SlideFret, MathUtil.Saturate((currentTime - note.TimeOffset) / note.TimeLength));

                        if (isDetected)
                            DrawVerticalImage(Layout.Current.GetImage("GuitarDetected"), slideFret - 0.5f, noteHeadTime, noteHeadHeight, stringColor, 0.1f);

                        DrawVerticalImage(Layout.Current.GetImage(imageName), slideFret - 0.5f, noteHeadTime, noteHeadHeight, stringColor, 0.08f);
                    }
                    else
                    {
                        if (isDetected)
                            DrawVerticalImage(Layout.Current.GetImage("GuitarDetected"), note.Fret - 0.5f, noteHeadTime, noteHeadHeight, stringColor, 0.1f);

                        DrawVerticalImage(Layout.Current.GetImage(imageName), note.Fret - 0.5f, noteHeadTime, noteHeadHeight, stringColor, 0.08f);
                    }
                }

                // Note modifier
                if (modifierImage != null)
                    DrawVerticalImage(modifierImage, note.Fret - 0.5f, noteHeadTime, noteHeadHeight, UIColor.White, 0.08f);
            }

            if (note.TimeOffset > currentTime)
                firstNote = note;
        }

        float scaleLength = 300.0f;

        float GetFretPosition(float fret)
        {
            return scaleLength - (scaleLength / (float)(Math.Pow(2, (double)fret / 12.0)));
        }

        int GetStringOffset(int str)
        {
            if (BassJamGame.Instance.Plugin.BassJamSaveState.SongPlayerSettings.InvertStrings)
                return numStrings - str - 1;

            return str;
        }

        float GetStringHeight(float str)
        {
            return 3.0f + (str * 4.0f);
        }

        float GetCentsOffset(float strng, float cents)
        {
            if (strng < 2)
                return cents / 30.0f;

            return cents / -30.0f;
        }

        float GetBendOffset(float startTime, float sustain, float strng, CentsOffset[] bendOffsets)
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

                    return GetCentsOffset(strng, MathUtil.Lerp((float)offset.Cents, (float)lastCents, timePercent));
                }

                lastCents = offset.Cents;
                lastTime = offset.TimeOffset;
            }

            return GetCentsOffset(strng,lastCents);
        }

        void DrawFretHorizontalLine(float startFret, float endFret, float time, float heightOffset, UIColor color, float imageScale)
        {
            startFret = GetFretPosition(startFret);
            endFret = GetFretPosition(endFret);
            time *= -timeScale;

            UIImage image = Layout.Current.GetImage("HorizontalFretLine");

            float minZ = time + ((float)image.Height * imageScale);
            float maxZ = time - ((float)image.Height * imageScale);

            DrawQuad(image, new Vector3(startFret, heightOffset, minZ), color, new Vector3(startFret, heightOffset, maxZ), color, new Vector3(endFret, heightOffset, maxZ), color, new Vector3(endFret, heightOffset, minZ), color);
        }

        void DrawFretVerticalLine(float fretCenter, float time, float startHeight, float endEndHeight, UIColor color)
        {
            fretCenter = GetFretPosition(fretCenter);
            time *= -timeScale;

            UIImage image = Layout.Current.GetImage("VerticalFretLine");

            float imageScale = .03f;

            float minX = (float)fretCenter - ((float)image.Width * imageScale);
            float maxX = (float)fretCenter + ((float)image.Width * imageScale);

            DrawQuad(image, new Vector3(minX, startHeight, time), color, new Vector3(minX, endEndHeight, time), color, new Vector3(maxX, endEndHeight, time), color, new Vector3(maxX, startHeight, time), color);
        }

        void DrawFretTimeLine(float fretCenter, float height, float startTime, float endTime, UIColor color)
        {
            fretCenter = GetFretPosition(fretCenter);
            startTime *= -timeScale;
            endTime *= -timeScale;

            UIImage image = Layout.Current.GetImage("VerticalFretLine");

            float imageScale = 0.03f;

            float minX = (float)fretCenter - ((float)image.Width * imageScale);
            float maxX = (float)fretCenter + ((float)image.Width * imageScale);

            DrawQuad(image, new Vector3(minX, height, startTime), color, new Vector3(minX, height, endTime), color, new Vector3(maxX, height, endTime), color, new Vector3(maxX, height, startTime), color);
        }

        void DrawVerticalImage(UIImage image, float startFret, float endFret, float time, float heightOffset, UIColor color, float imageScale)
        {
            startFret = GetFretPosition(startFret);
            endFret = GetFretPosition(endFret);
            time *= -timeScale;

            float minY = heightOffset - ((float)image.Height * imageScale);
            float maxY = heightOffset + ((float)image.Height * imageScale);

            DrawQuad(image, new Vector3(startFret, minY, time), color, new Vector3(startFret, maxY, time), color, new Vector3(endFret, maxY, time), color, new Vector3(endFret, minY, time), color);
        }

        void DrawVerticalImage(UIImage image, float fretCenter, float timeCenter, float heightOffset, UIColor color, float imageScale)
        {
            fretCenter = GetFretPosition(fretCenter);
            timeCenter *= -timeScale;

            float minX = fretCenter - ((float)image.Width * imageScale);
            float maxX = fretCenter + ((float)image.Width * imageScale);

            float minY = heightOffset - ((float)image.Height * imageScale);
            float maxY = heightOffset + ((float)image.Height * imageScale);

            DrawQuad(image, new Vector3(minX, minY, timeCenter), color, new Vector3(minX, maxY, timeCenter), color, new Vector3(maxX, maxY, timeCenter), color, new Vector3(maxX, minY, timeCenter), color);
        }

        void DrawVerticalImage(UIImage image, float startFret, float endFret, float time, float startHeight, float endHeight, UIColor color)
        {
            startFret = GetFretPosition(startFret);
            endFret = GetFretPosition(endFret);
            time *= -timeScale;

            DrawQuad(image, new Vector3(startFret, startHeight, time), color, new Vector3(startFret, endHeight, time), color, new Vector3(endFret, endHeight, time), color, new Vector3(endFret, startHeight, time), color);
        }

        void DrawFlatImage(UIImage image, float fretCenter, float startTime, float endTime, float heightOffset, UIColor color, float imageScale)
        {
            fretCenter = GetFretPosition(fretCenter);
            startTime *= -timeScale;
            endTime *= -timeScale;

            float minX = fretCenter - ((float)image.Width * imageScale);
            float maxX = fretCenter + ((float)image.Width * imageScale);

            DrawQuad(image, new Vector3(minX, heightOffset, startTime), color, new Vector3(minX, heightOffset, endTime), color, new Vector3(maxX, heightOffset, endTime), color, new Vector3(maxX, heightOffset, startTime), color);
        }

        void DrawFlatImage(UIImage image, float startFret, float endFret, float startTime, float endTime, float heightOffset, UIColor color)
        {
            startFret = GetFretPosition(startFret);
            endFret = GetFretPosition(endFret);
            startTime *= -timeScale;
            endTime *= -timeScale;

            DrawQuad(image, new Vector3(startFret, heightOffset, startTime), color, new Vector3(startFret, heightOffset, endTime), color, new Vector3(endFret, heightOffset, endTime), color, new Vector3(endFret, heightOffset, startTime), color);
        }

        void DrawSkewedFlatImage(UIImage image, float startFret, float endFret, float startTime, float endTime, float heightOffset, UIColor color)
        {
            startFret = GetFretPosition(startFret);
            endFret = GetFretPosition(endFret);
            startTime *= -timeScale;
            endTime *= -timeScale;

            float imageScale = .03f;

            DrawQuad(image, new Vector3(startFret - ((float)image.Width * imageScale), heightOffset, startTime), color, new Vector3(endFret - ((float)image.Width * imageScale), heightOffset, endTime), color,
                new Vector3(endFret + ((float)image.Width * imageScale), heightOffset, endTime), color, new Vector3(startFret + ((float)image.Width * imageScale), heightOffset, startTime), color);
        }

        void DrawImageTrail(UIImage image, UIColor color, float imageScale, params Vector3[] trailPoints)
        {
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

        void DrawVibrato(UIImage image, float fretCenter, float startTime, float endTime, float heightOffset, UIColor color)
        {
            float imageScale = .03f;

            fretCenter = GetFretPosition(fretCenter);

            float minX = fretCenter - ((float)image.Width * imageScale);
            float maxX = fretCenter + ((float)image.Width * imageScale);

            float lastTime = startTime;
            float lastHeight = heightOffset;

            int numPoints = (int)((endTime - startTime) / 0.02f);

            for (int i = 1; i <= numPoints; i++)
            {
                float time = MathUtil.Lerp(startTime, endTime, (float)i / (float)numPoints);

                if (time < currentTime)
                {
                    lastTime = currentTime;

                    continue;
                }

                float height = heightOffset + ((float)Math.Sin((time - startTime) * 50) * 1);

                DrawQuad(image, new Vector3(minX, lastHeight, lastTime * -timeScale), color,
                    new Vector3(minX, height, time * -timeScale), color,
                    new Vector3(maxX, height, time * -timeScale), color,
                    new Vector3(maxX, lastHeight, lastTime * -timeScale), color);

                lastHeight = height;
                lastTime = time;
            }
        }

        void DrawBend(UIImage image, float fretCenter, float startTime, float sustainTime, float strng, CentsOffset[] bendOffsets, UIColor color)
        {
            fretCenter = GetFretPosition(fretCenter);

            float heightOffset = GetStringHeight(strng);

            float imageScale = .03f;

            float minX = fretCenter - ((float)image.Width * imageScale);
            float maxX = fretCenter + ((float)image.Width * imageScale);

            float lastTime = startTime;
            float lastHeight = heightOffset;

            foreach (CentsOffset offset in bendOffsets)
            {
                float height = heightOffset + GetCentsOffset(strng, offset.Cents);

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

        void DrawFlatText(string text, float fretCenter, float timeCenter, float heightOffset, UIColor color, float imageScale)
        {
            fretCenter = GetFretPosition(fretCenter);
            timeCenter *= -timeScale;

            UIFont font = Layout.Current.GetFont("LargeFont");

            float textWidth;
            float textHeight;

            font.SpriteFont.MeasureString(text, out textWidth, out textHeight);

            textWidth *= imageScale;
            textHeight *= imageScale;

            float x = fretCenter - (textWidth / 2);

            Rectangle drawRect = Rectangle.Empty;

            for (int i = 0; i < text.Length; i++)
            {
                float z = timeCenter - (textHeight / 2);

                char c = text[i];

                SpriteFontGlyph glyph = font.SpriteFont.GetGlyph(c);

                drawRect.X = glyph.X;
                drawRect.Y = glyph.Y;
                drawRect.Width = glyph.Width;
                drawRect.Height = glyph.Height;

                DrawQuad(font.SpriteFont.FontImage, drawRect, new Vector3(x, heightOffset, z), color, new Vector3(x, heightOffset, z + ((float)glyph.Height * imageScale)), color,
                    new Vector3(x + ((float)glyph.Width * imageScale), heightOffset, z + ((float)glyph.Height * imageScale)), color,
                    new Vector3(x + ((float)glyph.Width * imageScale), heightOffset, z), color);

                x += (glyph.Width + font.SpriteFont.Spacing) * imageScale;
            }
        }

        void DrawVerticalText(string text, float fretCenter, float verticalCenter, float timeCenter, UIColor color, float imageScale)
        {
            fretCenter = GetFretPosition(fretCenter);
            timeCenter *= -timeScale;

            UIFont font = Layout.Current.GetFont("LargeFont");

            float textWidth;
            float textHeight;

            font.SpriteFont.MeasureString(text, imageScale, out textWidth, out textHeight);

            float x = fretCenter - (textWidth / 2);

            Rectangle drawRect = Rectangle.Empty;

            for (int i = 0; i < text.Length; i++)
            {
                float y = verticalCenter - (textHeight / 2);

                char c = text[i];

                SpriteFontGlyph glyph = font.SpriteFont.GetGlyph(c);

                drawRect.X = glyph.X;
                drawRect.Y = glyph.Y;
                drawRect.Width = glyph.Width;
                drawRect.Height = glyph.Height;

                DrawQuad(font.SpriteFont.FontImage, drawRect, new Vector3(x, y, timeCenter), color, new Vector3(x, y + ((float)glyph.Height * imageScale), timeCenter), color,
                    new Vector3(x + ((float)glyph.Width * imageScale), y + ((float)glyph.Height * imageScale), timeCenter), color, new Vector3(x + ((float)glyph.Width * imageScale), y, timeCenter), color);

                x += (glyph.Width + font.SpriteFont.Spacing) * imageScale;
            }
        }

        static double A4Frequency = 440.0;
        static int A4MidiNoteNum = 69;
        static double HalfStepRatio = Math.Pow(2.0, (1.0 / 12.0));

        static int[] GuitarStringNotes = { 40, 45, 50, 55, 59, 64 };
        static int[] BassStringNotes = { 28, 33, 38, 43 };

        const int FFTSize = 8192;
        Complex[] fftData = new Complex[FFTSize];
        float[] fftOutput = new float[FFTSize / 2];

        static double GetMidiNoteFrequency(int midiNoteNum)
        {
            int noteDiff = A4MidiNoteNum - midiNoteNum;

            return A4Frequency / Math.Pow(HalfStepRatio, noteDiff);
        }

        double GetNoteFrequency(int strng, int fret)
        {
            if (numStrings == 6)
            {
                return GetMidiNoteFrequency(GuitarStringNotes[strng] + fret);
            }
            else
            {
                return GetMidiNoteFrequency(BassStringNotes[strng] + fret);
            }
        }

        float GetPower(double frequency)
        {
            double bin = GetBin(frequency);
            
            int low = (int)Math.Floor(bin);

            double partial = bin - low;

            return (float)MathUtil.Lerp(fftOutput[low], fftOutput[low + 1], partial);
        }

        double GetBin(double frequency)
        {
            return (fftData.Length * (frequency / BassJamGame.Instance.Plugin.Host.SampleRate));
        }

        bool NoteDetect(SongNote note)
        {
            if (note.ChordID != -1)
            {
                SongChord chord = player.SongInstrumentNotes.Chords[note.ChordID];

                int numNotes = 0;

                for (int str = 0; str < chord.Fingers.Count; str++)
                {
                    if ((chord.Fingers[str] != -1) || (chord.Frets[str] != -1))
                    {
                        numNotes++;
                    }
                }

                double[] freqs = new double[numNotes];

                int pos = 0;

                for (int str = 0; str < chord.Fingers.Count; str++)
                {
                    if ((chord.Fingers[str] != -1) || (chord.Frets[str] != -1))
                    {
                        freqs[pos++] = GetNoteFrequency(str, chord.Frets[str]);
                    }
                }

                bool detected = NoteDetect(freqs);

                if (detected)
                {

                }

                return detected;
            }

            return NoteDetect(GetNoteFrequency(note.String, note.Fret));
        }

        bool NoteDetect(params double[] frequencies)
        {
            SampleHistory<double> history = BassJamGame.Instance.Plugin.SampleHistory;

            history.Process(ConvertToComplex, fftData.Length);

            FastFourierTransform.FFT(true, (int)Math.Log(fftData.Length, 2.0), fftData);

            for (int i = 0; i < fftData.Length / 2; i++)
            {
                float fft = Math.Abs(fftData[i].X + fftData[i].Y);
                float fftMirror = Math.Abs(fftData[fftData.Length - i - 1].X + fftData[fftData.Length - i - 1].Y);

                fftOutput[i] = (fft + fftMirror) * (0.5f + (i / (fftData.Length * 2)));
            }

            int maxFrequencyBin = (int)GetBin((numStrings == 6) ? 2637 : 330);  // Max note frequency

            float totPower = 0;

            foreach (double freq in frequencies)
            {
                totPower += GetPower(freq);
            }

            if (totPower > .001)
            {
                var sorted = fftOutput.Take(maxFrequencyBin).Select((x, i) => (x, i)).OrderByDescending(x => x.x).Take(frequencies.Length * 3);

                int numInTop = 0;

                foreach (double freq in frequencies)
                {
                    int bin = (int)GetBin(freq);

                    if (sorted.Where(x => (x.i == bin) || (x.i == (bin + 1))).Any())
                    {
                        numInTop++;
                    }
                }


                if (frequencies.Length == 1)
                    return (numInTop == 1);

                return (numInTop >= frequencies.Length - 1);
            }

            return false;
        }

        void ConvertToComplex(ReadOnlySpan<double> samples, int offset)
        {
            for (int pos = 0; pos < samples.Length; pos++)
            {
                fftData[pos + offset].X = (float)samples[pos] * (float)FastFourierTransform.HammingWindow(pos + offset, fftData.Length);
                fftData[pos + offset].Y = 0;
            }
        }
    }
}
