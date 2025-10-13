using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using System.Xml.Serialization;
using AudioPlugSharp;
using Microsoft.Xna.Framework.Media;
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
        public string Arrangements { get; set; } = "";
        public float LengthSeconds { get; set; }
        public string LeadGuitarTuning { get; set; }
        public string RhythmGuitarTuning { get; set; }
        public string BassGuitarTuning { get; set; }
        public float[] SongDifficulty { get; set; }
        [XmlIgnore]
        [JsonIgnore]
        public SongStatsEntry[] Stats { get; set; } = new SongStatsEntry[Enum.GetValues(typeof(ESongInstrumentType)).Length];

        public bool HasTag(string tag, ESongInstrumentType instrument)
        {
            if (Stats[(int)instrument] == null)
                return false;

            if (Stats[(int)instrument].Tags == null)
                return false;

            return Stats[(int)instrument].Tags.Contains(tag);
        }
    }

    public class SongIndex
    {   public List<SongIndexEntry> Songs { get; private set; } = new();
        public SongStats[] Stats = new SongStats[Enum.GetValues(typeof(ESongInstrumentType)).Length];
        public string BasePath { get; private set; }

        Dictionary<string, int> tagDict = new Dictionary<string, int>();

        static int numInstrumentTypes = Enum.GetValues(typeof(ESongInstrumentType)).Length;

        public IEnumerable<string> AllTags
        {
            get
            {
                return tagDict.OrderByDescending(t => t.Value).Select(t => t.Key);
            }
        }

        public void AddTag(string tag)
        {
            if (tagDict.ContainsKey(tag))
                tagDict[tag]++;
            else
                tagDict[tag] = 1;
        }

        public SongIndex(string basePath, bool forceRescan)
        {
            this.BasePath = basePath;

            if (!string.IsNullOrEmpty(basePath))
            {
                string indexFile = Path.Combine(basePath, "index.json");

                if (File.Exists(indexFile) && !forceRescan)
                {
                    using (Stream indexStream = File.OpenRead(indexFile))
                    {
                        Songs = JsonSerializer.Deserialize<List<SongIndexEntry>>(indexStream);

                        if (Songs != null)
                        {
                            // Quick check for valid values
                            foreach (var song in Songs)
                            {
                                if (song.SongDifficulty == null)
                                {
                                    song.SongDifficulty = new float[numInstrumentTypes];
                                }
                                else if (song.SongDifficulty.Length != numInstrumentTypes)
                                {
                                    var old = song.SongDifficulty;

                                    song.SongDifficulty = new float[numInstrumentTypes];

                                    Array.Copy(old, song.SongDifficulty, old.Length);
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (Directory.Exists(basePath))
                    {
                        IndexFolder(basePath);

                        using (Stream indexStream = File.Create(indexFile))
                        {
                            JsonSerializer.Serialize(indexStream, Songs);
                        }
                    }
                }

                tagDict["*"] = 1;

                foreach (ESongInstrumentType type in Enum.GetValues(typeof(ESongInstrumentType)))
                {
                    Stats[(int)type] = SongStats.Load(basePath, type);

                    Dictionary<string, SongStatsEntry> statsDict = new();

                    foreach (SongStatsEntry entry in Stats[(int)type].Songs)
                    {
                        statsDict[entry.Song] = entry;

                        if (entry.Tags != null)
                        {
                            foreach (string tag in entry.Tags)
                            {
                                AddTag(tag);
                            }
                        }
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

        public SongStatsEntry GetSongStats(SongIndexEntry song, ESongInstrumentType instrumentType)
        {
            SongStatsEntry stats = Stats[(int)instrumentType].Songs.Where(s => s.Song == song.FolderPath).FirstOrDefault();

            if (stats == null)
            {
                stats = new SongStatsEntry { Song = song.FolderPath };

                Stats[(int)instrumentType].Songs.Add(stats);
            }

            if (song.Stats[(int)instrumentType] == null)
            {
                song.Stats[(int)instrumentType] = stats;
            }

            return stats;
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
                try
                {
                    AddSong(folderPath);
                }
                catch (Exception ex)
                {
                    Logger.Log("Failed to add song [" + folderPath + "] with error: " + ex.ToString());
                }
            }
        }

        void AddSong(string songPath)
        {
            using (Stream songStream = File.OpenRead(Path.Combine(songPath, "song.json")))
            {
                SongData song = JsonSerializer.Deserialize<SongData>(songStream, SerializationUtil.CondensedSerializerOptions);

                if (song != null)
                {
                    SongIndexEntry indexEntry = new SongIndexEntry()
                    {
                        SongName = song.SongName,
                        ArtistName = song.ArtistName,
                        AlbumName = song.AlbumName,
                        FolderPath = Path.GetRelativePath(BasePath, songPath),
                        LengthSeconds = song.SongLengthSeconds,
                        SongDifficulty = new float[numInstrumentTypes]
                    };

                    foreach (SongInstrumentPart part in song.InstrumentParts.OrderBy(s => s.InstrumentType))
                    {
                        if (indexEntry.SongDifficulty[(int)part.InstrumentType] == 0)
                        {
                            indexEntry.SongDifficulty[(int)part.InstrumentType] = part.SongDifficulty;
                        }

                        if (part.InstrumentType == ESongInstrumentType.LeadGuitar)
                        {
                            indexEntry.Arrangements += "L";

                            indexEntry.LeadGuitarTuning = GetTuning(song, part);
                        }
                        else if (part.InstrumentType == ESongInstrumentType.RhythmGuitar)
                        {
                            indexEntry.Arrangements += "R";

                            indexEntry.RhythmGuitarTuning = GetTuning(song, part);
                        }
                        else if (part.InstrumentType == ESongInstrumentType.BassGuitar)
                        {
                            indexEntry.Arrangements += "B";

                            indexEntry.BassGuitarTuning = GetTuning(song, part);
                        }
                        else if (part.InstrumentType == ESongInstrumentType.Drums)
                        {
                            indexEntry.Arrangements += "D";
                        }
                        else if (part.InstrumentType == ESongInstrumentType.Keys)
                        {
                            indexEntry.Arrangements += "K";
                        }
                    }

                    Songs.Add(indexEntry);
                }
            }
        }

        string GetTuning(SongData song, SongInstrumentPart part)
        {
            return part.Tuning.GetTuning() + ((part.CapoFret > 0) ? (" C" + part.CapoFret) : "") + ((song.A440CentsOffset != 0) ? "*" : "");
        }

        public string GetSongPath(SongIndexEntry indexEntry)
        {
            string folder = indexEntry.FolderPath;

            if (Path.DirectorySeparatorChar != '\\')
            {
                folder = folder.Replace('\\', Path.DirectorySeparatorChar);
            }           

            return Path.Combine(BasePath, folder);
        }

        public string GetAlbumPath(SongIndexEntry indexEntry)
        {
            return Path.Combine(GetSongPath(indexEntry), "albumart.png");
        }

        public UIImage GetAlbumImage(SongIndexEntry indexEntry)
        {
            UIImage image = null;

            try
            {
                using (Stream inputStream = File.OpenRead(GetAlbumPath(indexEntry)))
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
                    return JsonSerializer.Deserialize<SongData>(songStream, SerializationUtil.CondensedSerializerOptions);
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

        public void AddTag(string tag)
        {
            if (Tags == null)
                Tags = new List<string>();

            if (!Tags.Contains(tag))
                Tags.Add(tag);
        }

        public void RemoveTag(string tag)
        {
            if (Tags == null)
                return;

            Tags.Remove(tag);

            if (Tags.Count == 0)
                Tags = null;
        }
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
    }
}
