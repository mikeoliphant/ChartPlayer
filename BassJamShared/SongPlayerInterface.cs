using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using PixelEngine;
using SongFormat;

namespace BassJam
{
    public class SongPlayerInterface : PopupGameState
    {
        public static SongPlayerInterface Instance { get; private set; }

        string basePath = @"C:\Share\JamSongs";

        SongListDisplay songList = new SongListDisplay();
        SongIndex songIndex;

        Dock mainDock;
        SongPlayerScene3D scene3D = null;
        SongPlayer songPlayer;

        SongData songData;

        public SongPlayerInterface()
        {
            Instance = this;

            DrawScene = true;
            UpdateWhenPaused = false;

            mainDock = new Dock();

            Element = mainDock;

            songIndex = new SongIndex(basePath);

            songIndex.IndexSongs();

            songList.SetSongs(songIndex.Songs);

            TextTouchButton songsButton = new TextTouchButton("ShowSongs", "Songs");
            songsButton.HorizontalAlignment = EHorizontalAlignment.Left;
            songsButton.VerticalAlignment = EVerticalAlignment.Bottom;

            mainDock.Children.Add(songsButton);
        }

        public void SetSong(SongIndexEntry song, ESongInstrumentType instrumentType)
        {
            try
            {
                string songPath = Path.Combine(basePath, song.FolderPath);

                using (Stream songStream = File.OpenRead(Path.Combine(songPath, "song.json")))
                {
                    songData = JsonSerializer.Deserialize<SongData>(songStream);
                }

                foreach (SongInstrumentPart part in songData.InstrumentParts)
                {
                    if (part.InstrumentType == instrumentType)
                    {
                        songPlayer = new SongPlayer();
                        songPlayer.SetPlaybackSampleRate(BassJamGame.Instance.Plugin.Host.SampleRate);

                        songPlayer.SetSong(songPath, songData, part);

                        songPlayer.SeekTime(Math.Max(songPlayer.SongInstrumentNotes.Notes[0].TimeOffset - 2, 0));

                        BassJamGame.Instance.Plugin.SetSongPlayer(songPlayer);

                        scene3D = new SongPlayerScene3D(songPlayer, 3);

                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                (PixGame.Instance.CurrentGameState as PopupGameState).ShowContinuePopup("Failed to create song player: " + ex.ToString(), null);
            }
        }

        public override void Update(float secondsElapsed)
        {
            base.Update(secondsElapsed);

            if (PixGame.InputManager.WasClicked("ShowSongs", this))
            {
                ShowPopup(songList);
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();

            if (scene3D != null)
                scene3D.Draw();
        }
    }

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

                    float startWithMinSustain = startTime - 0.1f;

                    minFret = 24;
                    maxFret = 0;

                    PixColor lineColor = PixColor.White;

                    foreach (SongBeat beat in player.SongStructure.Beats.Where(b => (b.TimeOffset >= startTime && b.TimeOffset <= endTime)))
                    {
                        lineColor.A = beat.IsMeasure ? (byte)128 : (byte)64;

                        DrawFretHorizontalLine(0, 23, beat.TimeOffset, 0, lineColor);
                    }

                    IEnumerable<SongNote> notes = player.SongInstrumentNotes.Notes.Where(n => ((n.TimeOffset + n.TimeLength) >= startWithMinSustain) && (n.TimeOffset <= endTime));

                    SongNote? lastNote = null;

                    foreach (SongNote note in notes)
                    {
                        if ((note.TimeOffset > currentTime) && (note.Fret > 0) && (!lastNote.HasValue || (lastNote.Value.Fret != note.Fret) || ((note.TimeOffset - lastNote.Value.TimeOffset) > 1)))
                        {
                            DrawVerticalText(note.Fret.ToString(), note.Fret - 0.5f, 0, note.TimeOffset, PixColor.White, 0.12f);
                        }

                        lastNote = note;
                    }

                    foreach (SongNote note in notes.OrderByDescending(n => n.TimeOffset))
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
                        DrawFretVerticalLine(fret - 1, startTime, GetStringHeight(0), GetStringHeight(numStrings), whiteHalfAlpha);

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

            switch (note.String)
            {
                case 0:
                    imageName = "GuitarRed";
                    break;

                case 1:
                    imageName = "GuitarYellow";
                    break;

                case 2:
                    imageName = "GuitarCyan";
                    break;

                case 3:
                    imageName = "GuitarOrange";
                    break;

                case 4:
                    imageName = "GuitarGreen";
                    break;

                case 5:
                    imageName = "GuitarPurple";
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
                    DrawFlatImage(PixGame.Instance.GetImage(imageName), midAnchorFret, noteHeadTime, noteHeadTime + noteSustain, GetStringHeight(note.String), stringColor);
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
                        DrawSkewedFlatImage(PixGame.Instance.GetImage(imageName), note.Fret - 0.5f, slideTo - 0.5f, noteHeadTime, noteHeadTime + noteSustain, GetStringHeight(note.String), stringColor);
                    }
                    else
                    {
                        DrawFlatImage(PixGame.Instance.GetImage(imageName), note.Fret - 0.5f, noteHeadTime, noteHeadTime + noteSustain, GetStringHeight(note.String), stringColor);
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

                // Note head
                DrawVerticalImage(PixGame.Instance.GetImage(imageName), note.Fret - 0.5f, noteHeadTime, GetStringHeight(note.String), stringColor);

                // Note modifier
                if (modifierImage != null)
                    DrawVerticalImage(modifierImage, note.Fret - 0.5f, noteHeadTime, GetStringHeight(note.String), PixColor.White);
            }

            if (note.TimeOffset > currentTime)
                firstNote = note;
        }

        float GetFretPosition(float fret)
        {
            //  s – (s / (2 ^ (n / 12)))

            return 240.0f - (240.0f / (float)(Math.Pow(2, (double)fret / 12.0)));
        }

        float GetStringHeight(int stringIndex)
        {
            return 3 + (stringIndex * 4);
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

            DrawQuad(image, new Vector3(minX, heightOffset, startTime), color, new Vector3(minX, heightOffset,  endTime), color, new Vector3(maxX, heightOffset, endTime), color, new Vector3(maxX, heightOffset, startTime), color);
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

            font.MeasureString(text, out textWidth, out textHeight);

            textWidth *= imageScale;
            textHeight *= imageScale;

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
