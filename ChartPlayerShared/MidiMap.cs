using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using SongFormat;

namespace ChartPlayer
{
    public struct DrumVoice
    {
        public EDrumKitPiece KitPiece;
        public EDrumArticulation Articulation;

        public DrumVoice(EDrumKitPiece kitPiece, EDrumArticulation articulation)
        {
            this.KitPiece = kitPiece;
            this.Articulation = articulation;
        }

        public override string ToString()
        {
            return String.Format("{0} {1}", KitPiece.ToString(), Articulation.ToString());
        }

        public override bool Equals(Object obj)
        {
            return obj is DrumVoice && this == (DrumVoice)obj;
        }

        public override int GetHashCode()
        {
            return KitPiece.GetHashCode() ^ Articulation.GetHashCode();
        }

        public static bool operator ==(DrumVoice x, DrumVoice y)
        {
            return (x.KitPiece == y.KitPiece) && (x.Articulation == y.Articulation);
        }

        public static bool operator !=(DrumVoice x, DrumVoice y)
        {
            return (x.KitPiece != y.KitPiece) || (x.Articulation != y.Articulation);
        }

        public static EDrumKitPieceType GetKitPieceType(EDrumKitPiece kitPiece)
        {
            switch (kitPiece)
            {
                case EDrumKitPiece.None:
                    return EDrumKitPieceType.None;
                case EDrumKitPiece.Kick:
                    return EDrumKitPieceType.Kick;
                case EDrumKitPiece.Snare:
                    return EDrumKitPieceType.Snare;
                case EDrumKitPiece.HiHat:
                    return EDrumKitPieceType.HiHat;
                case EDrumKitPiece.Crash:
                case EDrumKitPiece.Crash2:
                case EDrumKitPiece.Crash3:
                    return EDrumKitPieceType.Crash;
                case EDrumKitPiece.Ride:
                case EDrumKitPiece.Ride2:
                    return EDrumKitPieceType.Ride;
                case EDrumKitPiece.Tom1:
                case EDrumKitPiece.Tom2:
                case EDrumKitPiece.Tom3:
                case EDrumKitPiece.Tom4:
                case EDrumKitPiece.Tom5:
                    return EDrumKitPieceType.Tom;
                case EDrumKitPiece.Flexi1:
                case EDrumKitPiece.Flexi2:
                case EDrumKitPiece.Flexi3:
                case EDrumKitPiece.Flexi4:
                    return EDrumKitPieceType.Flexi;
            }

            return EDrumKitPieceType.None;
        }

        public static bool IsCompatible(EDrumKitPiece p1, EDrumKitPiece p2)
        {
            return GetKitPieceType(p1) == GetKitPieceType(p2);
        }

        public static string GetShortName(EDrumArticulation articulation)
        {
            switch (articulation)
            {
                case EDrumArticulation.None:
                    return "None";
                case EDrumArticulation.DrumHead:
                    return "Head";
                case EDrumArticulation.DrumHeadEdge:
                    return "HeadEdge";
                case EDrumArticulation.DrumRim:
                    return "Rim";
                case EDrumArticulation.SideStick:
                    return "SideStick";
                case EDrumArticulation.HiHatClosed:
                    return "Closed";
                case EDrumArticulation.HiHatOpen:
                    return "Open";
                case EDrumArticulation.HiHatChick:
                    return "Chick";
                case EDrumArticulation.HiHatSplash:
                    return "Splash";
                case EDrumArticulation.CymbalEdge:
                    return "Edge";
                case EDrumArticulation.CymbalBow:
                    return "Bow";
                case EDrumArticulation.CymbalBell:
                    return "Bell";
                case EDrumArticulation.CymbalChoke:
                    return "Choke";
                case EDrumArticulation.FlexiA:
                    return "FlexiA";
                case EDrumArticulation.FlexiB:
                    return "FlexiB";
                case EDrumArticulation.FlexiC:
                    return "FlexiC";
            }

            return articulation.ToString();
        }

        public static EDrumArticulation GetDefaultArticulation(EDrumKitPiece kitPiece)
        {
            return GetDefaultArticulation(GetKitPieceType(kitPiece));
        }

        public static EDrumArticulation GetDefaultArticulation(EDrumKitPieceType kitPieceType)
        {
            EDrumArticulation articulation = EDrumArticulation.None;

            switch (kitPieceType)
            {
                case EDrumKitPieceType.Kick:
                    articulation = EDrumArticulation.DrumHead;
                    break;
                case EDrumKitPieceType.Snare:
                    articulation = EDrumArticulation.DrumHead;
                    break;
                case EDrumKitPieceType.HiHat:
                    articulation = EDrumArticulation.CymbalEdge;
                    break;
                case EDrumKitPieceType.Crash:
                    articulation = EDrumArticulation.CymbalEdge;
                    break;
                case EDrumKitPieceType.Ride:
                    articulation = EDrumArticulation.CymbalBow;
                    break;
                case EDrumKitPieceType.Flexi:
                    articulation = EDrumArticulation.FlexiA;
                    break;
                case EDrumKitPieceType.Tom:
                    articulation = EDrumArticulation.DrumHead;
                    break;
                default:
                    break;
            }

            return articulation;
        }

        public static float GetDefaultDimensionValue(EDrumKitPieceType kitPieceType)
        {
            if (kitPieceType == EDrumKitPieceType.HiHat)
                return 1.0f;

            return 0;
        }

        public static IEnumerable<EDrumArticulation> GetValidArticulations(EDrumKitPieceType kitPieceType)
        {
            switch (kitPieceType)
            {
                case EDrumKitPieceType.None:
                    yield return EDrumArticulation.None;
                    break;
                case EDrumKitPieceType.Kick:
                    yield return EDrumArticulation.DrumHead;
                    break;
                case EDrumKitPieceType.Snare:
                    yield return EDrumArticulation.DrumHead;
                    yield return EDrumArticulation.DrumHeadEdge;
                    yield return EDrumArticulation.DrumRim;
                    yield return EDrumArticulation.SideStick;
                    break;
                case EDrumKitPieceType.HiHat:
                    yield return EDrumArticulation.HiHatOpen;
                    yield return EDrumArticulation.HiHatClosed;
                    yield return EDrumArticulation.CymbalEdge;
                    yield return EDrumArticulation.CymbalBow;
                    yield return EDrumArticulation.CymbalBell;
                    yield return EDrumArticulation.HiHatChick;
                    yield return EDrumArticulation.HiHatSplash;
                    break;
                case EDrumKitPieceType.Crash:
                case EDrumKitPieceType.Ride:
                    yield return EDrumArticulation.CymbalBow;
                    yield return EDrumArticulation.CymbalEdge;
                    yield return EDrumArticulation.CymbalBell;
                    yield return EDrumArticulation.CymbalChoke;
                    break;
                case EDrumKitPieceType.Tom:
                    yield return EDrumArticulation.DrumHead;
                    yield return EDrumArticulation.DrumRim;
                    break;
                case EDrumKitPieceType.Flexi:
                    yield return EDrumArticulation.FlexiA;
                    yield return EDrumArticulation.FlexiB;
                    yield return EDrumArticulation.FlexiC;
                    break;
            }
        }
    }

    public struct DrumHit
    {
        public DrumVoice Voice;
        public float Velocity;
        public bool IsLive;
        public float DimensionValue;

        public override string ToString()
        {
            return String.Format("Voice: {0} Velocity {1}", Voice.ToString(), Velocity);
        }
    }

    public class DrumMidiMapEntry
    {
        public int MidiNote { get; set; }
        public DrumVoice DrumVoice { get; set; }
    }

    public class DrumMidiDeviceConfiguration
    {
        public static DrumMidiDeviceConfiguration GenericMap { get; private set; }
        public static DrumMidiDeviceConfiguration CurrentMap { get; set; }

        Dictionary<int, DrumVoice> midiMap = new Dictionary<int, DrumVoice>();

        [XmlIgnore]
        public string Name { get; set; }
        public int HiHatPedalChannel { get; set; }
        public float HiHatPedalClosed { get; set; }
        public float HiHatPedalSemiOpen { get; set; }
        public float HiHatPedalOpen { get; set; }
        public int SnarePositionChannel { get; set; }
        public float SnarePositionCenter { get; set; }
        public float SnarePositionEdge { get; set; }
        public float SnareHotSpotCompensation { get; set; }

        public List<DrumMidiMapEntry> MidiMapEntrys { get; set; }

        static DrumMidiDeviceConfiguration()
        {
            CurrentMap = GenericMap = Generic();
        }

        static DrumMidiDeviceConfiguration Generic()
        {
            DrumMidiDeviceConfiguration map = new DrumMidiDeviceConfiguration();

            map.Name = "Generic";

            map.midiMap[35] = new DrumVoice(EDrumKitPiece.Kick, EDrumArticulation.DrumHead);
            map.midiMap[36] = new DrumVoice(EDrumKitPiece.Kick, EDrumArticulation.DrumHead);
            map.midiMap[38] = new DrumVoice(EDrumKitPiece.Snare, EDrumArticulation.DrumHead);
            map.midiMap[37] = new DrumVoice(EDrumKitPiece.Snare, EDrumArticulation.SideStick);
            map.midiMap[40] = new DrumVoice(EDrumKitPiece.Snare, EDrumArticulation.DrumHead);
            map.midiMap[48] = new DrumVoice(EDrumKitPiece.Tom1, EDrumArticulation.DrumHead);
            map.midiMap[45] = new DrumVoice(EDrumKitPiece.Tom2, EDrumArticulation.DrumHead);
            map.midiMap[43] = new DrumVoice(EDrumKitPiece.Tom3, EDrumArticulation.DrumHead);
            map.midiMap[47] = new DrumVoice(EDrumKitPiece.Tom4, EDrumArticulation.DrumHead);
            map.midiMap[46] = new DrumVoice(EDrumKitPiece.HiHat, EDrumArticulation.HiHatOpen);
            map.midiMap[42] = new DrumVoice(EDrumKitPiece.HiHat, EDrumArticulation.HiHatClosed);
            map.midiMap[44] = new DrumVoice(EDrumKitPiece.HiHat, EDrumArticulation.HiHatChick);
            map.midiMap[51] = new DrumVoice(EDrumKitPiece.Ride, EDrumArticulation.CymbalBow);
            map.midiMap[53] = new DrumVoice(EDrumKitPiece.Ride, EDrumArticulation.CymbalBell);
            map.midiMap[59] = new DrumVoice(EDrumKitPiece.Ride, EDrumArticulation.CymbalEdge);
            map.midiMap[49] = new DrumVoice(EDrumKitPiece.Crash, EDrumArticulation.CymbalEdge);
            map.midiMap[57] = new DrumVoice(EDrumKitPiece.Crash2, EDrumArticulation.CymbalEdge);
            map.midiMap[55] = new DrumVoice(EDrumKitPiece.Crash3, EDrumArticulation.CymbalEdge);

            return map;
        }

        public DrumMidiDeviceConfiguration()
        {
            HiHatPedalChannel = 4;
            HiHatPedalClosed = 1.0f;
            HiHatPedalSemiOpen = 0.5f;
            HiHatPedalOpen = 0.0f;

            SnarePositionChannel = 16;
            SnarePositionCenter = 0.6f;
            SnarePositionEdge = 0.8f;
        }


        public DrumVoice GetVoiceFromMidiNote(int midiNote)
        {
            if (!midiMap.ContainsKey(midiNote))
                return new DrumVoice(EDrumKitPiece.None, EDrumArticulation.None);

            return midiMap[midiNote];
        }

        public int GetMidiNoteFromDrumVoice(DrumVoice voice)
        {
            foreach (int midiNote in midiMap.Keys)
            {
                if (midiMap[midiNote] == voice)
                {
                    return midiNote;
                }
            }

            return 0;
        }

        //public IEnumerable<MidiMessage> GetMidiFromHit(DrumHit hit)
        //{
        //    int midiNote = GetMidiNoteFromDrumVoice(hit.Voice);

        //    if (midiNote == 0)
        //        midiNote = GetMidiNoteFromDrumVoice(new DrumVoice(hit.Voice.KitPiece, DrumVoice.GetDefaultArticulation(hit.Voice.KitPiece)));

        //    if (midiNote != 0)
        //    {
        //        if (hit.Voice.Articulation == EDrumArticulation.DrumHead)
        //        {
        //            yield return new MidiMessage(EMidiChannelCommand.Controller, 9, SnarePositionChannel, (int)(hit.DimensionValue * 127));
        //        }
        //        else if (hit.Voice.Articulation == EDrumArticulation.HiHatOpen)
        //        {
        //            float hatPedal = HiHatPedalOpen + (hit.DimensionValue * (HiHatPedalClosed - HiHatPedalOpen));

        //            yield return new MidiMessage(EMidiChannelCommand.Controller, 9, HiHatPedalChannel, (int)(hatPedal * 127));
        //        }

        //        yield return new MidiMessage(EMidiChannelCommand.NoteOn, 9, midiNote, (int)(hit.Velocity * 127));
        //    }
        //    else if (hit.Voice.Articulation == EDrumArticulation.CymbalChoke)
        //    {
        //        midiNote = GetMidiNoteFromDrumVoice(new DrumVoice(hit.Voice.KitPiece, DrumVoice.GetDefaultArticulation(hit.Voice.KitPiece)));

        //        if (midiNote != 0)
        //        {
        //            yield return new MidiMessage(EMidiChannelCommand.PolyPressure, 9, midiNote, (int)(hit.Velocity * 127));
        //        }
        //    }
        //}

        public void SetVoice(int midiNoteNumber, DrumVoice voice)
        {
            midiMap[midiNoteNumber] = voice;
        }

        float pedalValue;
        float snarePositionValue;

        public void SetHiHatPedalValue(float pedalValue)
        {
            this.pedalValue = pedalValue;
        }

        public DrumHit? HandleNoteOn(int channel, int noteNumber, float velocity, int sampleOffset, bool isLive)
        {
            DrumHit hit = new DrumHit()
            {
                IsLive = isLive
            };

            hit.Voice = GetVoiceFromMidiNote(noteNumber);

            if (hit.Voice.KitPiece != EDrumKitPiece.None)
            {
                hit.Velocity = velocity;

                if (hit.IsLive)
                {
                    if (hit.Voice.KitPiece == EDrumKitPiece.HiHat)
                    {
                        hit.DimensionValue = HiHatPedalOpen + (pedalValue * (HiHatPedalClosed - HiHatPedalOpen));
                    }
                    else if (hit.Voice.KitPiece == EDrumKitPiece.Snare)
                    {
                        hit.DimensionValue = snarePositionValue;
                    }
                }
                else
                {
                    if (hit.Voice.KitPiece == EDrumKitPiece.HiHat)
                    {
                        hit.DimensionValue = (hit.Voice.Articulation == EDrumArticulation.HiHatOpen) ? 0 : 1;
                    }
                }

                return hit;
            }

            return null;
        }

        public DrumHit? HandlePolyPressure(int channel, int noteNumber, float pressure, int sampleOffset, bool isLive)
        {
            DrumHit hit = new DrumHit()
            {
                IsLive = isLive
            };

            hit.Voice = GetVoiceFromMidiNote(noteNumber);

            if (hit.Voice.KitPiece != EDrumKitPiece.None)
            {
                if ((hit.Voice.KitPiece == EDrumKitPiece.Ride) || (hit.Voice.KitPiece == EDrumKitPiece.Crash) ||
                    (hit.Voice.KitPiece == EDrumKitPiece.Crash2) || (hit.Voice.KitPiece == EDrumKitPiece.Crash3))
                {
                    hit.Voice.Articulation = EDrumArticulation.CymbalChoke;
                    hit.Velocity = pressure;

                    return hit;
                }
            }

            return null;
        }

        public static DrumMidiDeviceConfiguration LoadFromXml(string path)
        {
            DrumMidiDeviceConfiguration drumMidiConfiguration = null;

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(DrumMidiDeviceConfiguration));

                using (Stream inputStream = File.OpenRead(path))
                {
                    drumMidiConfiguration = serializer.Deserialize(inputStream) as DrumMidiDeviceConfiguration;
                }

                foreach (DrumMidiMapEntry entry in drumMidiConfiguration.MidiMapEntrys)
                {
                    drumMidiConfiguration.midiMap[entry.MidiNote] = entry.DrumVoice;
                }

                drumMidiConfiguration.Name = Path.GetFileNameWithoutExtension(path);
            }
            catch
            {
            }

            return drumMidiConfiguration;
        }

        public void SaveXml(string path)
        {
            MidiMapEntrys = new List<DrumMidiMapEntry>();

            foreach (int noteNumber in midiMap.Keys)
            {
                MidiMapEntrys.Add(new DrumMidiMapEntry { MidiNote = noteNumber, DrumVoice = midiMap[noteNumber] });
            }

            XmlSerializer serializer = new XmlSerializer(typeof(DrumMidiDeviceConfiguration));

            using (Stream outputStream = File.Create(path))
            {
                serializer.Serialize(outputStream, this);
            }
        }
    }
}
