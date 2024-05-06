using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Xna.Framework;
using UILayout;
using SongFormat;
using System.Runtime.InteropServices;
using SharpDX.Direct2D1;
using System.Xml.Serialization;

namespace ChartPlayer
{
    public class SongPlayerInterface : Dock
    {
        public static SongPlayerInterface Instance { get; private set; }

        SongListDisplay songList = new SongListDisplay();
        SongIndex songIndex;

        SongPlayer songPlayer;
        SongSectionInterface sectionInterface;

        SongData songData;
        VocalDisplay vocalText;

        TextBlock songNameText;
        TextBlock songArtistText;
        TextBlock songInstrumentText;

        TextBlock speedText;
        HorizontalSlider speedSlider;

        TextToggleButton hideNotesButton;

        string globalSaveFolder;
        string globalOptionsFile;

        string songBasePath = null;

        public SongPlayerInterface()
        {
            Instance = this;

            globalSaveFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ChartPlayer");
            globalOptionsFile = Path.Combine(globalSaveFolder, "DefaultOptions.xml");

            if (!Directory.Exists(globalSaveFolder))
            {
                try
                {
                    Directory.CreateDirectory(globalSaveFolder);
                }
                catch { }
            }

            if (ChartPlayerGame.Instance.Plugin.ChartPlayerSaveState.SongPlayerSettings == null)
            {
                ChartPlayerGame.Instance.Plugin.ChartPlayerSaveState.SongPlayerSettings = LoadDefaultOptions();
            }

            songBasePath = ChartPlayerGame.Instance.Plugin.ChartPlayerSaveState.SongPlayerSettings.SongPath;

            songIndex = new SongIndex(songBasePath, forceRescan: false);

            songList.SetSongIndex(songIndex);

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

            vocalText = new VocalDisplay()
            {
                Margin = new LayoutPadding(20)
            };
            topStack.Children.Add(vocalText);

            HorizontalStack bottomButtonStack = new HorizontalStack()
            {
                Padding = new LayoutPadding(5),
                HorizontalAlignment = EHorizontalAlignment.Left,
                VerticalAlignment = EVerticalAlignment.Bottom,
                ChildSpacing = 2
            };
            Children.Add(bottomButtonStack);

            TextButton songsButton = new TextButton("Songs")
            {
                ClickAction = delegate
                {
                    if (songIndex.Songs.Count == 0)
                    {
                        ChartPlayerGame.Instance.ShowContinuePopup("No songs found.\n\nMake sure you have configured your Song Path in \"Options\".");
                    }
                    else
                    {
                        songList.SetCurrentInstrument(ChartPlayerGame.Instance.Plugin.ChartPlayerSaveState.SongPlayerSettings.CurrentInstrument);

                        ChartPlayerGame.Instance.Plugin.GameHost.IsMouseVisible = true;

                        Layout.Current.ShowPopup(songList);
                    }
                }
            };
            bottomButtonStack.Children.Add(songsButton);

            TextButton optionsButton = new TextButton("Options")
            {
                ClickAction = delegate
                {
                    ChartPlayerGame.Instance.Plugin.GameHost.IsMouseVisible = true;

                    Layout.Current.ShowPopup(new SongPlayerSettingsInterface(ChartPlayerGame.Instance.Plugin.ChartPlayerSaveState.SongPlayerSettings) { ApplyAction = ApplySettings });
                }
            };
            bottomButtonStack.Children.Add(optionsButton);

            NinePatchWrapper speedInterface = new NinePatchWrapper(Layout.Current.GetImage("ButtonUnpressed"))
            {
                VerticalAlignment = EVerticalAlignment.Stretch
            };
            bottomButtonStack.Children.Add(speedInterface);

            HorizontalStack speedStack = new HorizontalStack()
            {
                VerticalAlignment = EVerticalAlignment.Stretch
            };
            speedInterface.Child = speedStack;

            speedStack.Children.Add(speedText = new TextBlock("Speed: 100%")
            {
                VerticalAlignment = EVerticalAlignment.Center
            });
           
            speedSlider = new HorizontalSlider("HorizontalSlider")
            {
                VerticalAlignment = EVerticalAlignment.Center,
                DesiredWidth = 100,
                BackgroundColor = UIColor.Black,
                ChangeAction = SpeedChanged
            };
            speedSlider.SetLevel(1.0f);
            speedStack.Children.Add(speedSlider);

            hideNotesButton = new TextToggleButton("Show Notes", "Hide Notes")
            {
                ClickAction = ToggleNotes
            };
            bottomButtonStack.Children.Add(hideNotesButton);

            VerticalStack songInfoStack = new VerticalStack()
            {
                BackgroundColor = UIColor.Black,
                Padding = new LayoutPadding(5),
                HorizontalAlignment = EHorizontalAlignment.Right,
                VerticalAlignment = EVerticalAlignment.Bottom,
                ChildSpacing = 2
            };

            Children.Add(songInfoStack);

            songNameText = new TextBlock();
            songInfoStack.Children.Add(songNameText);

            songArtistText = new TextBlock();
            songInfoStack.Children.Add(songArtistText);

            songInstrumentText = new TextBlock();
            songInfoStack.Children.Add(songInstrumentText);
        }

        void ToggleNotes()
        {
            if ((ChartPlayerGame.Instance.Scene3D as FretPlayerScene3D) != null)
            {
                (ChartPlayerGame.Instance.Scene3D as FretPlayerScene3D).DisplayNotes = !hideNotesButton.IsPressed;
            }
        }

        void SpeedChanged(float newSpeed)
        {
            newSpeed = 0.5f + (newSpeed * 0.5f);

            if (songPlayer != null)
            {
                songPlayer.SetPlaybackSpeed(newSpeed);
            }

            speedText.Text = "Speed: " + (newSpeed * 100).ToString("0") + "%";
        }

        public void RescanSongIndex()
        {
            songIndex = new SongIndex(songBasePath, forceRescan: true);

            songList.SetSongIndex(songIndex);
        }

        public SongPlayerSettings LoadDefaultOptions()
        {
            SongPlayerSettings settings = null;

            if (File.Exists(globalOptionsFile))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(SongPlayerSettings));

                    using (Stream inputStream = File.OpenRead(globalOptionsFile))
                    {
                        settings = serializer.Deserialize(inputStream) as SongPlayerSettings;
                    }
                }
                catch
                {

                }
            }

            if (settings == null)
            {
                settings = new SongPlayerSettings();

                SaveDefaultOptions(settings);
            }

            return settings;
        }

        public void SaveDefaultOptions(SongPlayerSettings settings)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(SongPlayerSettings));

            try
            {
                using (Stream outputStream = File.Create(globalOptionsFile))
                {
                    serializer.Serialize(outputStream, settings);
                }
            }
            catch { }
        }

        public void Exit()
        {
            if ((ChartPlayerGame.Instance.Scene3D as FretPlayerScene3D) != null)
            {
                (ChartPlayerGame.Instance.Scene3D as FretPlayerScene3D).Stop();
            }
        }

        public void ResizeScreen()
        {
        }

        public void SetSong(SongIndexEntry song, ESongInstrumentType instrumentType, string partName)
        {
            ChartPlayerGame.Instance.Plugin.ChartPlayerSaveState.SongPlayerSettings.CurrentInstrument = songList.CurrentInstrument;

            try
            {
                string songPath = Path.Combine(songBasePath, song.FolderPath);

                using (Stream songStream = File.OpenRead(Path.Combine(songPath, "song.json")))
                {
                    songData = JsonSerializer.Deserialize<SongData>(songStream, SongIndex.SerializerOptions);
                }

                SongInstrumentPart part = null;

                if (!string.IsNullOrEmpty(partName))
                {
                    part = songData.InstrumentParts.Where(p => p.InstrumentName == partName).FirstOrDefault();
                }
                else
                {
                    part = songData.InstrumentParts.OrderBy(p => p.InstrumentName).Where(p => p.InstrumentType == instrumentType).FirstOrDefault();
                }

                songPlayer = new SongPlayer();
                songPlayer.SetPlaybackSampleRate(ChartPlayerGame.Instance.Plugin.Host.SampleRate);
                SpeedChanged(speedSlider.Level);
                songPlayer.RetuneToEStandard = ChartPlayerGame.Instance.Plugin.ChartPlayerSaveState.SongPlayerSettings.RetuneToEStandard;

                songPlayer.SetSong(songPath, songData, part);

                //songPlayer.SeekTime(Math.Max(songPlayer.SongInstrumentNotes.Notes[0].TimeOffset - 2, 0));

                ChartPlayerGame.Instance.Plugin.SetSongPlayer(songPlayer);

                sectionInterface.SetSongPlayer(songPlayer);

                vocalText.SongPlayer = songPlayer;

                if (part.InstrumentType == ESongInstrumentType.Keys)
                {
                    ChartPlayerGame.Instance.Scene3D = new KeysPlayerScene3D(songPlayer, 3);
                }
                else
                {
                    if ((ChartPlayerGame.Instance.Scene3D as FretPlayerScene3D) != null)
                    {
                        (ChartPlayerGame.Instance.Scene3D as FretPlayerScene3D).Stop();
                    }

                    ChartPlayerGame.Instance.Scene3D = new FretPlayerScene3D(songPlayer, 3);

                    (ChartPlayerGame.Instance.Scene3D as FretPlayerScene3D).DisplayNotes = !hideNotesButton.IsPressed;
                }

                SongStatsEntry stats = songIndex.Stats[(int)songList.CurrentInstrument].GetSongStats(song.FolderPath);

                if (song.Stats[(int)songList.CurrentInstrument] == null)
                {
                    song.Stats[(int)songList.CurrentInstrument] = stats;
                }

                stats.LastPlayed = DateTime.Now;
                stats.NumPlays++;

                songIndex.SaveStats();

                songNameText.Text = song.SongName;
                songArtistText.Text = song.ArtistName;
                songInstrumentText.Text = songPlayer.SongInstrumentPart.ToString();

                UpdateContentLayout();

                GC.Collect();
            }
            catch (Exception ex)
            {
                Layout.Current.ShowContinuePopup("Failed to create song player: " + ex.ToString(), null);
            }
        }

        [DllImport("kernel32.dll")]
        public static extern uint SetThreadExecutionState(uint esFlags);
        public const uint ES_CONTINUOUS = 0x80000000;
        public const uint ES_SYSTEM_REQUIRED = 0x00000001;
        public const uint ES_DISPLAY_REQUIRED = 0x00000002;

        Vector2 lastMousePosition;
        int mouseIdleFrames = 0;

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

            Vector2 mousePosition = inputManager.MousePosition;

            if (Vector2.Distance(mousePosition, lastMousePosition) > 10)
            {
                mouseIdleFrames = 0;

                ChartPlayerGame.Instance.Plugin.GameHost.IsMouseVisible = true;

                lastMousePosition = mousePosition;
            }
            else
            {
                mouseIdleFrames++;
                
                if (mouseIdleFrames > 200)
                {
                    ChartPlayerGame.Instance.Plugin.GameHost.IsMouseVisible = false;
                }
            }

            // Keep the monitor from turning off
            SetThreadExecutionState(ES_DISPLAY_REQUIRED | ES_SYSTEM_REQUIRED);

            //vocalText.FontScale = (float)PixGame.Instance.ScreenHeight / 800f;
        }


        void ApplySettings(SongPlayerSettings settings)
        {
            if (songPlayer != null)
            {
                songPlayer.RetuneToEStandard = settings.RetuneToEStandard;
            }

            // Check if song path changed
            if (settings.SongPath != songBasePath)
            {
                songBasePath = settings.SongPath;

                songIndex = new SongIndex(songBasePath, forceRescan: false);

                songList.SetSongIndex(songIndex);
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

            if (songPlayer.SongInstrumentNotes.Sections.Count == 0)
                return;

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

            if ((touch.TouchState == ETouchState.Pressed) || (touch.TouchState == ETouchState.Moved))
            {
                float time = endTime * ((touch.Position.X - ContentBounds.X) / ContentBounds.Width);

                if (Layout.Current.InputManager.IsDown("PreciseClick"))
                {
                    songPlayer.SeekTime(time);
                }
                else if (touch.TouchState == ETouchState.Pressed)
                {
                    foreach (SongSection section in songPlayer.SongInstrumentNotes.Sections)
                    {
                        if ((time >= section.StartTime) && (time < section.EndTime))
                        {
                            songPlayer.SeekTime(section.StartTime);
                        }
                    }
                }

                return true;
            }

            return false;
        }

        public override void HandleInput(InputManager inputManager)
        {
            base.HandleInput(inputManager);

            if (inputManager.WasClicked("FastForward", this))
            {
                songPlayer.SeekTime(songPlayer.CurrentSecond + 0.2f);
            }
            else if (inputManager.WasClicked("Rewind", this))
            {
                songPlayer.SeekTime(songPlayer.CurrentSecond - 0.2f);
            }
        }

        protected override void DrawContents()
        {
            base.DrawContents();

            if (songPlayer == null)
                return;

            UIColor color = UIColor.Orange;

            UIColor lineColor = UIColor.White;
            lineColor.A = 128;

            float currentTime = songPlayer.CurrentSecond;

            for (int i = 0; i < songPlayer.SongInstrumentNotes.Sections.Count; i++)
            {
                SongSection section = songPlayer.SongInstrumentNotes.Sections[i];

                bool isCurrent = (currentTime >= section.StartTime) && (currentTime < section.EndTime);

                if (isCurrent)
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

            int playPixel = (int)(((float)currentTime / endTime) * ContentBounds.Width);

            Layout.Current.GraphicsContext.DrawRectangle(new RectF(ContentBounds.X + playPixel - 1, (int)ContentBounds.Top, 2, (int)ContentBounds.Height), lineColor);
        }
    }
}
