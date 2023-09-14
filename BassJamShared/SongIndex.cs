using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using SongFormat;

namespace BassJam
{
    public class SongIndexEntry
    {
        public string SongName { get; set; }
        public string ArtistName { get; set; }
        public string AlbumName { get; set; }
        public string FolderPath { get; set; }
        public string LeadGuitarTuning { get; set; }
        public string RhythmGuitarTuning { get; set; }
        public string BassGuitarTuning { get; set; }
    }

    public class SongIndex
    {
        public List<SongIndexEntry> Songs { get; private set; } = new List<SongIndexEntry>();

        string basePath;

        public SongIndex(string basePath)
        {
            this.basePath = basePath;
        }

        public void IndexSongs()
        {
            IndexFolder(basePath);
        }

        void IndexFolder(string folderPath)
        {
            foreach (string subfolder in Directory.GetDirectories(folderPath))
            {
                IndexFolder(subfolder);
            }

            string songFile = Path.Combine(folderPath, "song.json");

            if (File.Exists(songFile))
            {
                AddSong(folderPath);
            }
        }

        void AddSong(string songPath)
        {
            using (Stream songStream = File.OpenRead(Path.Combine(songPath, "song.json")))
            {
                SongData song = JsonSerializer.Deserialize<SongData>(songStream);

                if (song != null)
                {
                    SongIndexEntry indexEntry = new SongIndexEntry()
                    {
                        SongName = song.SongName,
                        ArtistName = song.ArtistName,
                        AlbumName = song.AlbumName,
                        FolderPath = Path.GetRelativePath(basePath, songPath)
                    };

                    foreach (SongInstrumentPart part in song.InstrumentParts)
                    {
                        if (part.InstrumentType == ESongInstrumentType.LeadGuitar)
                        {
                            indexEntry.LeadGuitarTuning = part.Tuning.GetTuning();
                        }
                        else if (part.InstrumentType == ESongInstrumentType.RhythmGuitar)
                        {
                            indexEntry.RhythmGuitarTuning = part.Tuning.GetTuning();
                        }
                        else if (part.InstrumentType == ESongInstrumentType.BassGuitar)
                        {
                            indexEntry.BassGuitarTuning = part.Tuning.GetTuning();
                        }
                    }

                    Songs.Add(indexEntry);
                }
            }
        }
    }
}
