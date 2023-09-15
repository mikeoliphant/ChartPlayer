using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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
        TextBlock vocalText;
        List<int> sectionDensity = new List<int>();

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

            vocalText = new TextBlock
            {
                Font = PixGame.Instance.GetFont("LargeFont"),
                TextColor = PixColor.White,
                HorizontalPadding = 20,
                VerticalPadding = 20
            };

            mainDock.Children.Add(vocalText);

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

                        sectionDensity.Clear();

                        foreach (SongSection section in songPlayer.SongStructure.Sections)
                        {
                            SongNote? lastNote = null;

                            int density = 0;

                            foreach (SongNote note in songPlayer.SongInstrumentNotes.Notes.Where(n => ((n.TimeOffset >= section.StartTime) && (n.TimeOffset < section.EndTime))))
                            {
                                if (note.Techniques.HasFlag(ESongNoteTechnique.Chord))
                                {
                                    if (lastNote.HasValue && lastNote.Value.Techniques.HasFlag(ESongNoteTechnique.Chord) && (note.ChordID != lastNote.Value.ChordID))
                                        density += 4;
                                    else
                                        density += 2;
                                }
                                else
                                {
                                    if (lastNote.HasValue && (lastNote.Value.Fret != note.Fret))
                                        density += 2;
                                    else
                                        density += 1;

                                }
                            }

                            sectionDensity.Add(density);
                        }

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
        }

        public override void PostDraw()
        {
            base.PostDraw();

            if (scene3D != null)
                scene3D.Draw();
        }
    }

}
