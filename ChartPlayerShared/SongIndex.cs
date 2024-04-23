using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using SongFormat;
using UILayout;

namespace ChartPlayer
{
    public class SongIndexEntry
    {
        public string SongName { get; set; }
        public string ArtistName { get; set; }
        public string AlbumName { get; set; }
        public string FolderPath { get; set; }
        public string Arrangements { get; set; }
        public string LeadGuitarTuning { get; set; }
        public string RhythmGuitarTuning { get; set; }
        public string BassGuitarTuning { get; set; }
        public string KeysTuning { get; set; }
        [XmlIgnore]
        [JsonIgnore]
        public SongStatsEntry[] Stats { get; set; } = new SongStatsEntry[Enum.GetValues(typeof(ESongInstrumentType)).Length];
    }

    public class SongIndex
    {
        public static JsonSerializerOptions SerializerOptions { get; } =
            new JsonSerializerOptions()
            {
                Converters = {
                   new JsonStringEnumConverter()
                },
            };

        public List<SongIndexEntry> Songs { get; private set; } = new List<SongIndexEntry>();
        public SongStats[] Stats = new SongStats[Enum.GetValues(typeof(ESongInstrumentType)).Length];
        public string BasePath { get; private set; }

        public SongIndex(string basePath)
        {
            this.BasePath = basePath;

            if (!string.IsNullOrEmpty(basePath))
            {
                string indexFile = Path.Combine(basePath, "index.json");

                if (File.Exists(indexFile))
                {
                    using (Stream indexStream = File.OpenRead(indexFile))
                    {
                        Songs = JsonSerializer.Deserialize<List<SongIndexEntry>>(indexStream);
                    }
                }
                else
                {
                    IndexFolder(basePath);

                    using (Stream indexStream = File.Create(indexFile))
                    {
                        JsonSerializer.Serialize(indexStream, Songs);
                    }
                }

                foreach (ESongInstrumentType type in Enum.GetValues(typeof(ESongInstrumentType)))
                {
                    Stats[(int)type] = SongStats.Load(basePath, type);

                    Dictionary<string, SongStatsEntry> statsDict = new();

                    foreach (SongStatsEntry entry in Stats[(int)type].Songs)
                    {
                        statsDict[entry.Song] = entry;
                    }

                    foreach (SongIndexEntry indexEntry in Songs)
                    {
                        if (statsDict.ContainsKey(indexEntry.FolderPath))
                        {
                            indexEntry.Stats[(int)type] = statsDict[indexEntry.FolderPath];
                        }
                    }
                }
            }
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
            using (Stream songStream = File.OpenRead(Path.Combine(BasePath, "song.json")))
            {
                SongData song = JsonSerializer.Deserialize<SongData>(songStream, SongIndex.SerializerOptions);

                if (song != null)
                {
                    SongIndexEntry indexEntry = new SongIndexEntry()
                    {
                        SongName = song.SongName,
                        ArtistName = song.ArtistName,
                        AlbumName = song.AlbumName,
                        FolderPath = Path.GetRelativePath(BasePath, songPath)
                    };
                    
                    foreach (SongInstrumentPart part in song.InstrumentParts.OrderBy(s => s.InstrumentType))
                    {
                        if (part.InstrumentType == ESongInstrumentType.LeadGuitar)
                        {
                            indexEntry.Arrangements += "L";

                            indexEntry.LeadGuitarTuning = GetTuning(part);
                        }
                        else if (part.InstrumentType == ESongInstrumentType.RhythmGuitar)
                        {
                            indexEntry.Arrangements += "R";

                            indexEntry.RhythmGuitarTuning = GetTuning(part);
                        }
                        else if (part.InstrumentType == ESongInstrumentType.BassGuitar)
                        {
                            indexEntry.Arrangements += "B";

                            indexEntry.BassGuitarTuning = GetTuning(part);
                        }
                        else if (part.InstrumentType == ESongInstrumentType.Keys)
                        {
                            indexEntry.Arrangements += "K";

                            indexEntry.KeysTuning = "Standard";
                        }
                    }

                    Songs.Add(indexEntry);
                }
            }
        }

        string GetTuning(SongInstrumentPart part)
        {
            return part.Tuning.GetTuning() + ((part.CapoFret > 0) ? (" C" + part.CapoFret) : "");
        }

        public string GetSongPath(SongIndexEntry indexEntry)
        {
            return Path.Combine(BasePath, indexEntry.FolderPath);
        }

        public UIImage GetAlbumImage(SongIndexEntry indexEntry)
        {
            UIImage image = null;

            try
            {
                using (Stream inputStream = File.OpenRead(Path.Combine(GetSongPath(indexEntry), "albumart.png")))
                {
                    image = new UIImage(inputStream);
                }
            }
            catch
            {
                image = new UIImage(Layout.Current.GetImage("SingleWhitePixel"));
            }

            return image;
        }

        public SongData GetSongData(SongIndexEntry indexEntry)
        {
            try
            {
                using (Stream songStream = File.OpenRead(Path.Combine(GetSongPath(indexEntry), "song.json")))
                {
                    return JsonSerializer.Deserialize<SongData>(songStream, SongIndex.SerializerOptions);
                }
            }
            catch
            {

            }

            return null;
        }

        public void SaveStats()
        {
            for (int i = 0; i < Stats.Length; i++)
            {
                if (Stats[i].Songs.Count > 0)
                    Stats[i].Save();
            }
        }
    }

    public class SongStatsEntry
    {
        public string Song { get; set; }
        public DateTime LastPlayed { get; set; } = DateTime.MinValue;
        public int NumPlays { get; set; } = 0;
        public List<string> Tags { get; set; } = null;
    }

    public class SongStats
    {
        public List<SongStatsEntry> Songs { get; set; } = new List<SongStatsEntry>();

        string filename = null;

        public static SongStats Load(string basePath, ESongInstrumentType instrumentType)
        {
            SongStats stats = new SongStats();

            string statsFile = Path.Combine(basePath, "stats" + instrumentType + ".json");

            if (File.Exists(statsFile))
            {
                try
                {
                    using (Stream statStream = File.OpenRead(statsFile))
                    {
                        stats = JsonSerializer.Deserialize<SongStats>(statStream);
                    }
                }
                catch { }
            }

            stats.filename = statsFile;

            return stats;
        }

        public void Save()
        {
            if (string.IsNullOrEmpty(filename))
            {
                throw new InvalidOperationException();
            }

            using (Stream indexStream = File.Create(filename))
            {
                JsonSerializer.Serialize(indexStream, this);
            }
        }

        public SongStatsEntry GetSongStats(string songPath)
        {
            SongStatsEntry stats = Songs.Where(s => s.Song == songPath).FirstOrDefault();

            if (stats == null)
            {
                stats = new SongStatsEntry { Song = songPath };

                Songs.Add(stats);
            }

            return stats;
        }
    }
}
