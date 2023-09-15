using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Xna.Framework;
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
        SongSectionInterface sectionInterface;

        SongData songData;
        TextBlock vocalText;

        public SongPlayerInterface()
        {
            Instance = this;

            DrawScene = true;
            UpdateWhenPaused = false;

            mainDock = new Dock();

            Element = mainDock;

            songIndex = new SongIndex(basePath);

            songList.SetSongs(songIndex.Songs);

            VerticalStack topStack = new VerticalStack()
            {
                HorizontalAlignment = EHorizontalAlignment.Stretch,
                VerticalAlignment = EVerticalAlignment.Top
            };
            mainDock.Children.Add(topStack);

            sectionInterface = new SongSectionInterface()
            {
                HorizontalAlignment = EHorizontalAlignment.Center,
                VerticalAlignment = EVerticalAlignment.Top,
                VerticalPadding = 20
            };
            topStack.Children.Add(sectionInterface);

            vocalText = new TextBlock
            {
                Font = PixGame.Instance.GetFont("LargeFont"),
                TextColor = PixColor.White,
                HorizontalPadding = 20,
                VerticalPadding = 20
            };

            topStack.Children.Add(vocalText);

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

                        sectionInterface.SetSongPlayer(songPlayer);

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

            if (songPlayer != null)
            {
                float endTime = (float)songPlayer.CurrentSecond + 2;

                vocalText.StringBuilder.Clear();

                foreach (SongVocal vocal in songPlayer.SongVocals.Where(v => (v.TimeOffset >= songPlayer.CurrentSecond) && (v.TimeOffset <= endTime)))
                {
                    vocalText.StringBuilder.Append(vocal.Vocal);

                    if (!vocal.Vocal.EndsWith('\n'))
                    {
                        vocalText.StringBuilder.Append(' ');
                    }
                }
            }

            vocalText.FontScale = (float)PixGame.Instance.ScreenHeight / 800f;
        }

        public override void PostDraw()
        {
            base.PostDraw();

            if (scene3D != null)
                scene3D.Draw();
        }
    }

    public class SongSectionInterface : Dock
    {
        SongPlayer songPlayer;
        List<float> sectionDensity = new List<float>();
        float maxDensity;
        float endTime;

        public SongSectionInterface()
        {

        }

        public void SetSongPlayer(SongPlayer songPlayer)
        {
            this.songPlayer = songPlayer;

            sectionDensity.Clear();

            foreach (SongSection section in songPlayer.SongInstrumentNotes.Sections)
            {
                SongNote? lastNote = null;
                int density = 0;

                foreach (SongNote note in songPlayer.SongInstrumentNotes.Notes.Where(n => ((n.TimeOffset >= section.StartTime) && (n.TimeOffset < section.EndTime))))
                {
                    if (note.Techniques.HasFlag(ESongNoteTechnique.Chord))
                    {
                        if (lastNote.HasValue && lastNote.Value.Techniques.HasFlag(ESongNoteTechnique.Chord) && (note.ChordID != lastNote.Value.ChordID))
                            density += 5;
                        else
                            density += 1;
                    }
                    else
                    {
                        if (lastNote.HasValue && (lastNote.Value.Fret != note.Fret))
                            density += 5;
                        else
                            density += 1;

                    }
                }

                sectionDensity.Add((float)density / (section.EndTime - section.StartTime));
            }

            maxDensity = sectionDensity.Max();

            endTime = songPlayer.SongInstrumentNotes.Sections.Last().EndTime;
        }

        protected override void GetContentSize(out float width, out float height)
        {
            width = (float)PixGame.Instance.ScreenWidth * .7f;
            height = (float)PixGame.Instance.ScreenHeight * .1f;
        }

        public override bool HandleTouch(PixTouch touch)
        {
            if (touch.TouchState != EPixTouchState.Pressed)
                return false;

            float time = endTime * ((touch.Position.X - ContentLayout.Offset.X) / ContentLayout.Width);

            foreach (SongSection section in songPlayer.SongInstrumentNotes.Sections)
            {
                if ((time >= section.StartTime) && (time < section.EndTime))
                {
                    songPlayer.SeekTime(section.StartTime);
                }
            }

            return true;
        }

        protected override void DrawContents()
        {
            base.DrawContents();

            if (songPlayer == null)
                return;

            Rectangle drawRect;

            PixColor color = PixColor.Orange;

            float currentTime = songPlayer.CurrentSecond;

            for (int i = 0; i < songPlayer.SongInstrumentNotes.Sections.Count; i++)
            {
                SongSection section = songPlayer.SongInstrumentNotes.Sections[i];

                if ((currentTime >= section.StartTime) && (currentTime < section.EndTime))
                    color.A = 255;
                else
                    color.A = 192;

                float startPercent = section.StartTime / endTime;
                float endPercent = section.EndTime / endTime;

                int startX = (int)(ContentLayout.Offset.X + (ContentLayout.Width * startPercent));

                int endX = (int)(ContentLayout.Offset.X + (ContentLayout.Width * endPercent));

                float height = ContentLayout.Height * ((float)sectionDensity[i] / (float)maxDensity);

                drawRect = new Rectangle((int)startX, (int)(ContentLayout.Bottom - (int)height), endX - startX - 2, (int)height);

                PixGame.Instance.UIScene.DrawRectangle(ref drawRect, 0.5f, color);
            }
        }
    }
}
