using System;
using System.Collections.Generic;
using System.Linq;

namespace SongFormat
{
    /// <summary>
    /// Top level song metadata
    /// </summary>
    public class SongData
    {
        public string SongName { get; set; }
        public string ArtistName { get; set; }
        public string AlbumName { get; set; }
        public List<SongInstrumentPart> InstrumentParts { get; set; } = new List<SongInstrumentPart>();
        public float A440CentsOffset { get; set; }

        public SongInstrumentPart GetPart(string instrumentName)
        {
            return InstrumentParts.FirstOrDefault(p => p.InstrumentName == instrumentName);
        }
    }

    public class SongStructure
    {
        // Sections (verse, chorus)
        // Measures/beats
        public List<SongBeat> Beats { get; set; } = new List<SongBeat>();
    }

    public struct SongBeat
    {
        public float TimeOffset { get; set; }
        public bool IsMeasure { get; set; }
    }

    public enum ESongInstrumentType
    {
        LeadGuitar,
        RhythmGuitar,
        BassGuitar,
        Vocals
    }

    public class SongInstrumentPart
    {
        public string InstrumentName { get; set; }
        public ESongInstrumentType InstrumentType { get; set; }
        public StringTuning Tuning { get; set; }
    }

    public class StringTuning
    {
        public List<int> StringSemitoneOffsets { get; set; } = null;

        public string GetTuning()
        {
            // If we don't have any offsets, assume E Standard
            if ((StringSemitoneOffsets == null) || (StringSemitoneOffsets.Count < 4))
                return "E Std";

            if (IsOffsetFromStandard())
            {
                string key = GetOffsetNote(StringSemitoneOffsets[1]);

                if (key == null)
                    return "Custom";

                if (StringSemitoneOffsets[0] == StringSemitoneOffsets[1])
                {
                    return key + " Std";
                }
                else // Drop tuning
                {
                    string drop = GetDropNote(StringSemitoneOffsets[0]);

                    if (drop == null)
                        return "Custom";

                    if (key == "E")
                        return "Drop " + drop;

                    return key + " Drop " + drop;
                }
            }

            return "Custom";
        }

        public bool IsOffsetFromStandard()
        {
            for (int i = 2; i < StringSemitoneOffsets.Count; i++)
                if (StringSemitoneOffsets[1] != StringSemitoneOffsets[i])
                    return false;

            return true;
        }

        public static string GetOffsetNote(int offset)
        {
            switch (offset)
            {
                case 0:
                    return "E";
                case 1:
                    return "F";
                case 2:
                    return "F#";
                case -1:
                    return "Eb";
                case -2:
                    return "D";
                case -3:
                    return "C#";
                case -4:
                    return "C";
                case -5:
                    return "B";
            }

            return null;
        }

        public static string GetDropNote(int offset)
        {
            switch (offset)
            {
                case -1:
                    return "Eb";
                case -2:
                    return "D";
                case -3:
                    return "Db";
                case -4:
                    return "C";
                case -5:
                    return "B";
            }

            return null;
        }
    }

    public class SongInstrumentNotes
    {
        public List<SongChord> Chords { get; set; } = new List<SongChord>();
        public List<SongNote> Notes { get; set; } = new List<SongNote>();
    }

    public class SongChord
    {
        public string Name { get; set; }
        public List<int> Fingers { get; set; } = new List<int>();
        public List<int> Frets { get; set;} = new List<int>();
    }

    public struct SongNote
    {
        public float TimeOffset { get; set; } = 0;
        public float TimeLength { get; set; } = 0;
        public int Fret { get; set; } = -1;
        public int String { get; set; } = -1;
        public ESongNoteTechnique Techniques { get; set; } = 0;
        public int HandFret { get; set; } = -1;
        public int SlideFret { get; set; } = -1;
        public int ChordID { get; set; } = -1;

        public SongNote()
        {
        }
    }

    [Flags]
    public enum ESongNoteTechnique
    {
        HammerOn = 1 << 1,
        PullOff = 1 << 2,
        Accent = 1 << 3,
        PalmMute = 1 << 4,
        FretHandMute = 1 << 5,
        Slide = 1 << 6,
        Tremolo = 1 << 7,
        Vibrato = 1 << 8,
        Harmonic = 1 << 9,
        PinchHarmonic = 1 << 10,
        Tap = 1 << 11,
        Slap = 1 << 12,
        Pop = 1 << 13,
        Chord = 1 << 14
    }

    public struct SongVocal
    {
        public string Vocal { get; set; }
        public float TimeOffset { get; set; }
    }
}
