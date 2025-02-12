using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Xna.Framework;
using UILayout;
using SongFormat;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Data.SqlTypes;
using System.Media;

namespace ChartPlayer
{
    public class SongPlayerInterface : Dock
    {
        public static SongPlayerInterface Instance { get; private set; }

        SongListDisplay songList = new SongListDisplay();
        SongIndex songIndex;

        SongPlayer songPlayer;
        SongSectionInterface sectionInterface;
        SongPlayerSettingsInterface settingsInterface;

        SongData songData;
        VocalDisplay vocalText;

        TextBlock songNameText;
        TextBlock songArtistText;
        TextBlock songInstrumentText;

        TextBlock speedText;
        HorizontalSlider speedSlider;

        TextToggleButton hideNotesButton;

        UIElementWrapper scoreTextWrapper;
        StringBuilderTextBlock scoreText;
        int lastTotalNotes = -1;
        int lastDetectedNotes = -1;

        bool needSeek = false;
        float seekSecs = 0;

        StringBuilderTextBlock playTimeText;

        int currentMinute = 0;
        int currentSecond = 0;

        NinePatchWrapper bpmInterface;
        StringBuilderTextBlock bpmText;
        float lastBPM = 0;

        int songBPM = 0;

        HorizontalSlider playTimeSlider;

        ImageToggleButton pauseButton;

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

            if (!string.IsNullOrEmpty(ChartPlayerGame.Instance.Plugin.ChartPlayerSaveState.SongPlayerSettings.SongListSortColumn))
            {
                songList.SetCurrentInstrument(ChartPlayerGame.Instance.Plugin.ChartPlayerSaveState.SongPlayerSettings.CurrentInstrument);
                songList.SongList.SetSortColumn(ChartPlayerGame.Instance.Plugin.ChartPlayerSaveState.SongPlayerSettings.SongListSortColumn);
                songList.SongList.CurrentSortReverse = ChartPlayerGame.Instance.Plugin.ChartPlayerSaveState.SongPlayerSettings.SongListSortReversed;
            }

            ChartPlayerGame.Instance.Scale = ChartPlayerGame.Instance.Plugin.ChartPlayerSaveState.SongPlayerSettings.UIScale;

            if (!string.IsNullOrEmpty(ChartPlayerGame.Instance.Plugin.ChartPlayerSaveState.SongPlayerSettings.DrumMidiMapName))
            {
                try
                {
                    var map = DrumMidiDeviceConfiguration.LoadFromXml(
                        Path.Combine(MidiEditor.ConfigPath, "MidiMaps", ChartPlayerGame.Instance.Plugin.ChartPlayerSaveState.SongPlayerSettings.DrumMidiMapName + ".xml"));

                    if (map != null)
                        DrumMidiDeviceConfiguration.CurrentMap = map;
                }
                catch { }
            }

            settingsInterface = new SongPlayerSettingsInterface(ChartPlayerGame.Instance.Plugin.ChartPlayerSaveState.SongPlayerSettings) { ApplyAction = ApplySettings };

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
                Padding = new LayoutPadding(0, 5)
            };
            topStack.Children.Add(sectionInterface);

            vocalText = new VocalDisplay()
            {
                Margin = new LayoutPadding(20, 5)
            };
            topStack.Children.Add(vocalText);

            HorizontalStack bottomStack = new HorizontalStack()
            {
                HorizontalAlignment = EHorizontalAlignment.Stretch,
                VerticalAlignment = EVerticalAlignment.Bottom,
            };
            children.Add(bottomStack);

            HorizontalStack bottomButtonStack = new HorizontalStack()
            {
                BackgroundColor = UIColor.Black.MultiplyAlpha(0.5f),
                Padding = new LayoutPadding(5),
                HorizontalAlignment = EHorizontalAlignment.Stretch,
                VerticalAlignment = EVerticalAlignment.Bottom,
                ChildSpacing = 2
            };
            bottomStack.Children.Add(bottomButtonStack);

            TextButton songsButton = new TextButton("Songs")
            {
                VerticalAlignment = EVerticalAlignment.Stretch,
                ClickAction = ShowSongs
            };
            bottomButtonStack.Children.Add(songsButton);

            TextButton optionsButton = new TextButton("Options")
            {
                VerticalAlignment = EVerticalAlignment.Stretch,
                ClickAction = delegate
                {
                    ChartPlayerGame.Instance.Plugin.GameHost.IsMouseVisible = true;

                    if (songPlayer != null)
                    {
                        if (!songPlayer.Paused)
                            TogglePaused();
                    }

                    Layout.Current.ShowPopup(settingsInterface);
                }
            };
            bottomButtonStack.Children.Add(optionsButton);

            TextButton helpButton = new TextButton("?")
            {
                VerticalAlignment = EVerticalAlignment.Stretch,
                ClickAction = delegate
                {
                    ChartPlayerGame.Instance.Plugin.GameHost.IsMouseVisible = true;

                    Layout.Current.ShowPopup(new HelpDialog(new TextBlock("<Space> to pause/resume.\n\nClick song outline at top of screen to skip to phrase.\n\nShift Click song outline to seek to exact position.\n\n" +
                        "Left/Right arrows to move forward/back.\n\n[ ] keys to toggle loop markers and loop section.")));
                }
            };
            bottomButtonStack.Children.Add(helpButton);

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
                VerticalAlignment = EVerticalAlignment.Stretch
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

            bpmInterface = new NinePatchWrapper(Layout.Current.GetImage("ButtonUnpressed"))
            {
                VerticalAlignment = EVerticalAlignment.Stretch
            };
            bottomButtonStack.Children.Add(bpmInterface);

            HorizontalStack bpmStack = new HorizontalStack()
            {
                VerticalAlignment = EVerticalAlignment.Stretch
            };
            bpmInterface.Child = bpmStack;

            bpmStack.Children.Add(bpmText = new StringBuilderTextBlock("BPM:")
            {
                DesiredWidth = 170,
                VerticalAlignment = EVerticalAlignment.Stretch
            });

            hideNotesButton = new TextToggleButton("Show Notes", "Hide Notes")
            {
                VerticalAlignment = EVerticalAlignment.Stretch,
                ClickAction = ToggleNotes
            };
            bottomButtonStack.Children.Add(hideNotesButton);

            NinePatchWrapper scoreInterface = new NinePatchWrapper(Layout.Current.GetImage("ButtonUnpressed"))
            {
                VerticalAlignment = EVerticalAlignment.Stretch
            };
            bottomButtonStack.Children.Add(scoreInterface);

            HorizontalStack scoreStack = new HorizontalStack()
            {
                VerticalAlignment = EVerticalAlignment.Stretch,
                DesiredWidth = 210,
                ChildSpacing = 10
            };
            scoreInterface.Child = scoreStack;

            scoreTextWrapper = new UIElementWrapper()
            {
                HorizontalAlignment = EHorizontalAlignment.Stretch
            };
            scoreStack.Children.Add(scoreTextWrapper);

            scoreText = new StringBuilderTextBlock("0/0")
            {
                HorizontalAlignment = EHorizontalAlignment.Center,
                VerticalAlignment = EVerticalAlignment.Center,
            };
            scoreTextWrapper.Child = scoreText;

            NinePatchWrapper playTimeInterface = new NinePatchWrapper(Layout.Current.GetImage("ButtonUnpressed"))
            {
                VerticalAlignment = EVerticalAlignment.Stretch,
                HorizontalAlignment = EHorizontalAlignment.Stretch
            };
            bottomButtonStack.Children.Add(playTimeInterface);

            HorizontalStack playTimeStack = new HorizontalStack()
            {
                VerticalAlignment = EVerticalAlignment.Stretch,
                HorizontalAlignment = EHorizontalAlignment.Stretch
            };
            playTimeInterface.Child = playTimeStack;

            pauseButton = new ImageToggleButton("Play", "Pause")
            {
                HorizontalAlignment = EHorizontalAlignment.Center,
                VerticalAlignment = EVerticalAlignment.Center,
                ClickAction = TogglePaused
            };
            playTimeStack.Children.Add(pauseButton);

            UIElementWrapper playTimeTextWrapper = new UIElementWrapper()
            {
                VerticalAlignment = EVerticalAlignment.Stretch,
                DesiredWidth = 80
            };
            playTimeStack.Children.Add(playTimeTextWrapper);

            playTimeText = new StringBuilderTextBlock("0:00")
            {
                HorizontalAlignment = EHorizontalAlignment.Center,
                VerticalAlignment = EVerticalAlignment.Center,
            };
            playTimeTextWrapper.Child = playTimeText;

            playTimeSlider = new HorizontalSlider("HorizontalSlider")
            {
                VerticalAlignment = EVerticalAlignment.Center,
                HorizontalAlignment = EHorizontalAlignment.Stretch,
                BackgroundColor = UIColor.Black,
                ChangeAction = PlayTimeChanged
            };
            playTimeSlider.SetLevel(0);
            playTimeStack.Children.Add(playTimeSlider);

            VerticalStack songInfoStack = new VerticalStack()
            {
                BackgroundColor = UIColor.Black.MultiplyAlpha(0.5f),
                Margin = new LayoutPadding(20, 0, 0, 0),
                Padding = new LayoutPadding(10),
                HorizontalAlignment = EHorizontalAlignment.Right,
                VerticalAlignment = EVerticalAlignment.Bottom,
                ChildSpacing = 2
            };

            bottomStack.Children.Add(songInfoStack);

            songNameText = new TextBlock();
            songInfoStack.Children.Add(songNameText);

            songArtistText = new TextBlock();
            songInfoStack.Children.Add(songArtistText);

            songInstrumentText = new TextBlock();
            songInfoStack.Children.Add(songInstrumentText);

            ShowSongs();
        }

        void ShowSongs()
        {
            if (songIndex.Songs.Count == 0)
            {
                ChartPlayerGame.Instance.ShowContinuePopup("No songs found.\n\nMake sure you have configured your Song Path in \"Options\".");
            }
            else
            {
                ChartPlayerGame.Instance.Plugin.GameHost.IsMouseVisible = true;

                Layout.Current.ShowPopup(songList);
            }
        }

        void ToggleNotes()
        {
            if ((ChartPlayerGame.Instance.Scene3D as FretPlayerScene3D) != null)
            {
                (ChartPlayerGame.Instance.Scene3D as FretPlayerScene3D).DisplayNotes = !hideNotesButton.IsPressed;
            }
        }

        void PlayTimeChanged(float newPlayPercent)
        {
            if (songPlayer != null)
            {
                SeekTime(songPlayer.SongLengthSeconds * newPlayPercent);
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
            ChartPlayerGame.Instance.Plugin.ChartPlayerSaveState.SongPlayerSettings.SongListSortColumn = (songList.SongList.CurrentSortColumn != null) ? songList.SongList.CurrentSortColumn.DisplayName : null;
            ChartPlayerGame.Instance.Plugin.ChartPlayerSaveState.SongPlayerSettings.SongListSortReversed = songList.SongList.CurrentSortReverse;

            try
            {
                string songPath = songIndex.GetSongPath(song);

                using (Stream songStream = File.OpenRead(Path.Combine(songPath, "song.json")))
                {
                    songData = JsonSerializer.Deserialize<SongData>(songStream, SerializationUtil.CondensedSerializerOptions);
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

                songPlayer.BasePath = songIndex.BasePath;
                songPlayer.SetPlaybackSampleRate(ChartPlayerGame.Instance.Plugin.Host.SampleRate);
                SpeedChanged(speedSlider.Level);
                songPlayer.SongTuningMode = ChartPlayerGame.Instance.Plugin.ChartPlayerSaveState.SongPlayerSettings.SongTuningMode;

                songPlayer.SetSong(songPath, songData, part);

                //songPlayer.SeekTime(Math.Max(songPlayer.SongInstrumentNotes.Notes[0].TimeOffset - 2, 0));

                ChartPlayerGame.Instance.Plugin.SetSongPlayer(songPlayer);

                sectionInterface.SetSongPlayer(songPlayer);

                vocalText.SongPlayer = songPlayer;
                if (part.InstrumentType == ESongInstrumentType.Drums)
                {
                    ChartPlayerGame.Instance.Scene3D = new DrumPlayerScene3D(songPlayer)
                    {
                        LeftyMode = ChartPlayerGame.Instance.Plugin.ChartPlayerSaveState.SongPlayerSettings.LeftyMode,
                        NoteDisplaySeconds = ChartPlayerGame.Instance.Plugin.ChartPlayerSaveState.SongPlayerSettings.DrumsNoteDisplaySeconds
                    };
                }
                else if (part.InstrumentType == ESongInstrumentType.Keys)
                {
                    ChartPlayerGame.Instance.Scene3D = new KeysPlayerScene3D(songPlayer)
                    {
                        LeftyMode = ChartPlayerGame.Instance.Plugin.ChartPlayerSaveState.SongPlayerSettings.LeftyMode,
                        NoteDisplaySeconds = ChartPlayerGame.Instance.Plugin.ChartPlayerSaveState.SongPlayerSettings.KeysNoteDisplaySeconds
                    };
                }
                else
                {
                    if ((ChartPlayerGame.Instance.Scene3D as FretPlayerScene3D) != null)
                    {
                        (ChartPlayerGame.Instance.Scene3D as FretPlayerScene3D).Stop();
                    }

                    ChartPlayerGame.Instance.Scene3D = new FretPlayerScene3D(songPlayer)
                    {
                        LeftyMode = ChartPlayerGame.Instance.Plugin.ChartPlayerSaveState.SongPlayerSettings.LeftyMode,
                        NoteDisplaySeconds = ChartPlayerGame.Instance.Plugin.ChartPlayerSaveState.SongPlayerSettings.NoteDisplaySeconds
                    };

                    (ChartPlayerGame.Instance.Scene3D as FretPlayerScene3D).DisplayNotes = !hideNotesButton.IsPressed;
                }

                SongStatsEntry stats = songIndex.GetSongStats(song, songList.CurrentInstrument);

                stats.LastPlayed = DateTime.Now;
                stats.NumPlays++;

                songIndex.SaveStats();

                songNameText.Text = song.SongName;
                songArtistText.Text = song.ArtistName;
                songInstrumentText.Text = songPlayer.SongInstrumentPart.ToString();

                float lastBeatTime = 0;

                Dictionary<int, int> beatHistogram = new();

                foreach (SongBeat beat in songPlayer.SongStructure.Beats)
                {
                    if (lastBeatTime > 0)
                    {
                        float delta = beat.TimeOffset - lastBeatTime;

                        int currentCount = 0;

                        int bpm = (int)Math.Round((1.0 / delta) * 60);

                        beatHistogram.TryGetValue(bpm, out currentCount);

                        beatHistogram[bpm] = currentCount + 1;
                    }

                    lastBeatTime = beat.TimeOffset;
                }

                songBPM = beatHistogram.OrderByDescending(b => b.Value).FirstOrDefault().Key;

                bpmText.StringBuilder.Clear();
                bpmText.StringBuilder.Append("BPM: ");
                bpmText.StringBuilder.AppendNumber(songBPM);

                UpdateContentLayout();

                GC.Collect();
            }
            catch (Exception ex)
            {
                Layout.Current.ShowContinuePopup("Failed to create song player: " + ex.ToString(), null);
            }
        }

#if WINDOWS
        [DllImport("kernel32.dll")]
        public static extern uint SetThreadExecutionState(uint esFlags);
        public const uint ES_CONTINUOUS = 0x80000000;
        public const uint ES_SYSTEM_REQUIRED = 0x00000001;
        public const uint ES_DISPLAY_REQUIRED = 0x00000002;
#endif

        Vector2 lastMousePosition;
        int mouseIdleFrames = 0;

        public override void HandleInput(InputManager inputManager)
        {
            base.HandleInput(inputManager);

            if (inputManager.WasPressed("PauseGame"))
            {
                TogglePaused();
            }

            if (inputManager.WasClicked("Exit", this))
            {
                ShowSongs();
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
#if WINDOWS
            SetThreadExecutionState(ES_DISPLAY_REQUIRED | ES_SYSTEM_REQUIRED);
#endif

            //vocalText.FontScale = (float)PixGame.Instance.ScreenHeight / 800f;
            if (inputManager.WasPressed("LoopMarkerStart"))
            {
                songPlayer.LoopMarkerStartSecond = songPlayer.CurrentSecond;               

            }
            if (inputManager.WasPressed("LoopMarkerEnd"))
            {
                songPlayer.LoopMarkerEndSecond = songPlayer.CurrentSecond;
            }
        }

        public void TogglePaused()
        {
            if (songPlayer != null)
            {
                songPlayer.Paused = !songPlayer.Paused;
            }

            pauseButton.SetPressed(songPlayer.Paused);
        }

        void ApplySettings(SongPlayerSettings settings)
        {
            if (songPlayer != null)
            {
                songPlayer.SongTuningMode = settings.SongTuningMode;
            }

            if ((ChartPlayerGame.Instance.Scene3D as ChartScene3D) != null)
            {
                (ChartPlayerGame.Instance.Scene3D as ChartScene3D).LeftyMode = ChartPlayerGame.Instance.Plugin.ChartPlayerSaveState.SongPlayerSettings.LeftyMode;

                (ChartPlayerGame.Instance.Scene3D as ChartScene3D).NoteDisplaySeconds = (ChartPlayerGame.Instance.Scene3D is FretPlayerScene3D) ? settings.NoteDisplaySeconds : settings.DrumsNoteDisplaySeconds;
            }

            // Check if song path changed
            if (settings.SongPath != songBasePath)
            {
                songBasePath = settings.SongPath;

                songIndex = new SongIndex(songBasePath, forceRescan: false);

                songList.SetSongIndex(songIndex);
            }

            ChartPlayerGame.Instance.Scale = settings.UIScale;
            Layout.Current.UpdateLayout();
        }

        public void SeekTime(float secs)
        {
            seekSecs = secs;
            needSeek = true;
        }

        void DoSeekTime(float secs)
        {
            songPlayer.SeekTime(secs);

            ChartScene3D fretScene = (ChartPlayerGame.Instance.Scene3D as ChartScene3D);

            if (fretScene != null)
            {
                fretScene.ResetScore(secs);
            }
        }

        public void CheckLoopMarkers()       
        {
            if (songPlayer.LoopMarkerStartSecond != -1 && songPlayer.LoopMarkerEndSecond != -1)
            {
                if (songPlayer.CurrentSecond > songPlayer.LoopMarkerEndSecond)
                {
                    SeekTime(songPlayer.LoopMarkerStartSecond);
                }
            }           
        }

        protected override void DrawContents()
        {
            if (songPlayer != null)
            {
                if (needSeek)
                {
                    DoSeekTime(seekSecs);

                    needSeek = false;
                }

                float newSecondFloat = songPlayer.CurrentSecond;

                int newMinute = (int)(newSecondFloat / 60);
                int newSecond = (int)newSecondFloat % 60;

                if ((newMinute != currentMinute) || (newSecond != currentSecond))
                {
                    currentMinute = newMinute;
                    currentSecond = newSecond;

                    playTimeText.StringBuilder.Clear();
                    playTimeText.StringBuilder.AppendNumber(currentMinute);
                    playTimeText.StringBuilder.Append(':');
                    playTimeText.StringBuilder.AppendNumber(currentSecond, 2);
                }

                playTimeSlider.SetLevel(newSecondFloat / songPlayer.SongLengthSeconds);

                CheckLoopMarkers();
            }

            ChartScene3D chartScene = (ChartPlayerGame.Instance.Scene3D as ChartScene3D);

            if (chartScene != null)
            {
                int totalNotes = chartScene.NumNotesTotal;
                int detectedNotes = chartScene.NumNotesDetected;

                if ((totalNotes != lastTotalNotes) || (detectedNotes != lastDetectedNotes))
                {
                    lastTotalNotes = totalNotes;
                    lastDetectedNotes = detectedNotes;

                    scoreText.StringBuilder.Clear();
                    scoreText.StringBuilder.AppendNumber(detectedNotes);
                    scoreText.StringBuilder.Append("/");
                    scoreText.StringBuilder.AppendNumber(totalNotes);

                    if (totalNotes > 0)
                    {
                        scoreText.StringBuilder.Append(" (");

                        float percent = (float)detectedNotes / (float)totalNotes;

                        int evenPercent = (int)(percent * 100);

                        scoreText.StringBuilder.AppendNumber(evenPercent);
                        scoreText.StringBuilder.Append('.');

                        percent -= evenPercent / 100;

                        scoreText.StringBuilder.AppendNumber((int)(percent * 10));
                        scoreText.StringBuilder.Append("%)");
                    }

                    scoreTextWrapper.UpdateContentLayout();
                }

                float bpm = chartScene.CurrentBPM * songPlayer.PlaybackSpeed;

                bpm = ((int)(bpm * 10)) / 10.0f;

                if (bpm != lastBPM)
                {
                    bpmText.StringBuilder.Clear();
                    bpmText.StringBuilder.Append("BPM: ");
                    bpmText.StringBuilder.AppendNumber((int)(Math.Round(songBPM * songPlayer.PlaybackSpeed)));
                    bpmText.StringBuilder.Append(" (");
                    bpmText.StringBuilder.AppendNumber((int)bpm);
                    bpmText.StringBuilder.Append('.');
                    bpmText.StringBuilder.AppendNumber((int)((bpm - Math.Floor(bpm)) * 10));
                    bpmText.StringBuilder.Append(')');

                    lastBPM = bpm;

                    bpmInterface.UpdateContentLayout();
                }
            }                      

            base.DrawContents();
        }
    }

    public class SongSectionInterface : Dock
    {
        SongPlayer songPlayer;
        List<float> sectionDensity = new List<float>();
        float maxDensity;
        float endTime;
        List<SongSection> sections;

        public SongSectionInterface()
        {
        }

        public void SetSongPlayer(SongPlayer songPlayer)
        {
            this.songPlayer = songPlayer;

            sectionDensity.Clear();

            sections = songPlayer.SongInstrumentNotes.Sections;

            if (sections.Count == 0)
            {
                sections = songPlayer.SongStructure.Sections;
            }

            if (sections.Count == 0)
                return;

            foreach (SongSection section in sections)
            {
                int density = 0;

                switch (songPlayer.SongInstrumentPart.InstrumentType)
                {
                    case ESongInstrumentType.LeadGuitar:
                    case ESongInstrumentType.RhythmGuitar:
                    case ESongInstrumentType.BassGuitar:
                        density = GetDensity(section, songPlayer.SongInstrumentNotes.Notes);
                        break;
                    case ESongInstrumentType.Keys:
                        density = GetDensity(section, songPlayer.SongKeyboardNotes.Notes);
                        break;
                    case ESongInstrumentType.Drums:
                        density = GetDensity(section, songPlayer.SongDrumNotes.Notes);
                        break;
                    case ESongInstrumentType.Vocals:
                        break;
                }

                sectionDensity.Add((float)density / (section.EndTime - section.StartTime));
            }

            maxDensity = sectionDensity.Max();

            endTime = sections.Last().EndTime;
        }

        int GetDensity<T>(SongSection section, IEnumerable<T> notes) where T : struct, ISongEvent
        {
            T? lastNote = null;

            int density = 0;

            var blah = notes.Where(n => ((n.TimeOffset >= section.StartTime) && (n.TimeOffset < section.EndTime))).ToList();

            foreach (var note in notes.Where(n => ((n.TimeOffset >= section.StartTime) && (n.TimeOffset < section.EndTime))))
            {
                if ((note is SongNote songNote) && (lastNote is SongNote lastSongNote))
                {
                    if (songNote.Techniques.HasFlag(ESongNoteTechnique.Chord))
                    {
                        density += (songNote.ChordID != lastSongNote.ChordID) ? 5 : 1;
                    }
                    else
                    {
                        density += (songNote.Fret != lastSongNote.Fret) ? 5 : 1;
                    }
                }
                else if ((note is SongDrumNote drumNote) && (lastNote is SongDrumNote lastDrumNote))
                {
                    density += ((drumNote.KitPiece == lastDrumNote.KitPiece) && (drumNote.Articulation == lastDrumNote.Articulation)) ? 5 : 1;
                }
                else if ((note is SongKeyboardNote keyNote) && (lastNote is SongKeyboardNote lastKeyNote))
                {
                    density += (keyNote.Note == lastKeyNote.Note) ? 5 : 1;
                }

                lastNote = note;
            }

            return density;
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
                    SongPlayerInterface.Instance.SeekTime(time);
                }
                else if (touch.TouchState == ETouchState.Pressed)
                {
                    foreach (SongSection section in sections)
                    {
                        if ((time >= section.StartTime) && (time < section.EndTime))
                        {
                            SongPlayerInterface.Instance.SeekTime(section.StartTime);
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
                SongPlayerInterface.Instance.SeekTime(songPlayer.CurrentSecond + 0.2f);
            }
            else if (inputManager.WasClicked("Rewind", this))
            {
                SongPlayerInterface.Instance.SeekTime(songPlayer.CurrentSecond - 0.2f);
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

            UIColor loopMarkerStartColor = UIColor.Green;
            loopMarkerStartColor.A = 128;
            UIColor loopMarkerEndColor = UIColor.Red;
            loopMarkerStartColor.A = 128;
            float currentTime = songPlayer.CurrentSecond;

            for (int i = 0; i < sections.Count; i++)
            {
                SongSection section = sections[i];

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
            int loopMarkerStartPixel = (int)(((float)songPlayer.LoopMarkerStartSecond / endTime) * ContentBounds.Width);
            int loopMarkerEndPixel = (int)(((float)songPlayer.LoopMarkerEndSecond / endTime) * ContentBounds.Width);

            Layout.Current.GraphicsContext.DrawRectangle(new RectF(ContentBounds.X + playPixel - 1, (int)ContentBounds.Top, 2, (int)ContentBounds.Height), lineColor);

            if (songPlayer.LoopMarkerStartSecond != -1)
            {
                Layout.Current.GraphicsContext.DrawRectangle(new RectF(ContentBounds.X + loopMarkerStartPixel - 1, (int)ContentBounds.Top, 2, (int)ContentBounds.Height), loopMarkerStartColor);
            }
            if (songPlayer.LoopMarkerEndSecond != -1)
            {
                Layout.Current.GraphicsContext.DrawRectangle(new RectF(ContentBounds.X + loopMarkerEndPixel - 1, (int)ContentBounds.Top, 2, (int)ContentBounds.Height), loopMarkerEndColor);
            }
          
        }
    }
}
