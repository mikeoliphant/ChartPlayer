using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Xna.Framework;
using UILayout;
using SongFormat;

namespace BassJam
{
    public class SongPlayerInterface : Dock
    {
        public static SongPlayerInterface Instance { get; private set; }

        string basePath = @"C:\Share\JamSongs";

        SongListDisplay songList = new SongListDisplay();
        SongIndex songIndex;

        SongPlayer songPlayer;
        SongSectionInterface sectionInterface;

        SongData songData;
        StringBuilderTextBlock vocalText;

        public SongPlayerInterface()
        {
            Instance = this;

            Padding = new LayoutPadding(5);

            songIndex = new SongIndex(basePath);

            songList.SetSongs(songIndex.Songs);

            VerticalStack topStack = new VerticalStack()
            {
                HorizontalAlignment = EHorizontalAlignment.Stretch,
                VerticalAlignment = EVerticalAlignment.Top
            };
            Children.Add(topStack);

            sectionInterface = new SongSectionInterface()
            {
                HorizontalAlignment = EHorizontalAlignment.Center,
                VerticalAlignment = EVerticalAlignment.Top,
                Padding = new LayoutPadding(0, 20)
            };
            topStack.Children.Add(sectionInterface);

            vocalText = new StringBuilderTextBlock
            {
                TextFont = BassJamGame.Instance.GetFont("LargeFont"),
                TextColor = UIColor.White,
                Padding = new LayoutPadding(20)
            };

            topStack.Children.Add(vocalText);

            HorizontalStack bottomButtonStack = new HorizontalStack()
            {
                HorizontalAlignment = EHorizontalAlignment.Left,
                VerticalAlignment = EVerticalAlignment.Bottom,
                ChildSpacing = 2
            };
            Children.Add(bottomButtonStack);

            TextButton songsButton = new TextButton("Songs")
            {
                ClickAction = delegate
                {
                    songList.SetCurrentInstrument(BassJamGame.Instance.Plugin.BassJamSaveState.SongPlayerSettings.CurrentInstrument);

                    Layout.Current.ShowPopup(songList);
                }
            };
            bottomButtonStack.Children.Add(songsButton);

            TextButton optionsButton = new TextButton("Options")
            {
                ClickAction = delegate { Layout.Current.ShowPopup(new SongPlayerSettingsInterface(BassJamGame.Instance.Plugin.BassJamSaveState.SongPlayerSettings) { ApplyAction = ApplySettings }); }
            };
            bottomButtonStack.Children.Add(optionsButton);
        }

        public void ResizeScreen()
        {
        }

        public void SetSong(SongIndexEntry song, ESongInstrumentType instrumentType)
        {
            BassJamGame.Instance.Plugin.BassJamSaveState.SongPlayerSettings.CurrentInstrument = songList.CurrentInstrument;

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
                        songPlayer.RetuneToEStandard = BassJamGame.Instance.Plugin.BassJamSaveState.SongPlayerSettings.RetuneToEStandard;

                        songPlayer.SetSong(songPath, songData, part);

                        songPlayer.SeekTime(Math.Max(songPlayer.SongInstrumentNotes.Notes[0].TimeOffset - 2, 0));

                        BassJamGame.Instance.Plugin.SetSongPlayer(songPlayer);

                        sectionInterface.SetSongPlayer(songPlayer);

                        BassJamGame.Instance.Scene3D = new SongPlayerScene3D(songPlayer, 3);

                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Layout.Current.ShowContinuePopup("Failed to create song player: " + ex.ToString(), null);
            }
        }

        public override void HandleInput(InputManager inputManager)
        {
            base.HandleInput(inputManager);
            
            if (inputManager.WasPressed("PauseGame"))
            {
                if (songPlayer != null)
                {
                    songPlayer.Paused = !songPlayer.Paused;
                }
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

            //vocalText.FontScale = (float)PixGame.Instance.ScreenHeight / 800f;
        }

        void ApplySettings()
        {
            if (songPlayer != null)
            {
                songPlayer.RetuneToEStandard = BassJamGame.Instance.Plugin.BassJamSaveState.SongPlayerSettings.RetuneToEStandard;
            }
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
            width = Layout.Current.Bounds.Width * .9f;
            height = Layout.Current.Bounds.Height * .1f;
        }

        public override bool HandleTouch(in Touch touch)
        {
            if (songPlayer == null)
                return false;

            if (touch.TouchState != ETouchState.Pressed)
                return false;

            float time = endTime * ((touch.Position.X - ContentBounds.X) / ContentBounds.Width);

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

            UIColor color = UIColor.Orange;

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

                int startX = (int)(ContentBounds.X + (ContentBounds.Width * startPercent));

                int endX = (int)(ContentBounds.X + (ContentBounds.Width * endPercent));

                float height = ContentBounds.Height * ((float)sectionDensity[i] / (float)maxDensity);

                Layout.Current.GraphicsContext.DrawRectangle(new RectF((int)startX, (int)(ContentBounds.Bottom - (int)height), endX - startX - 2, (int)height), color);
            }
        }
    }
}
