using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AudioPlugSharp;
using NVorbis;
using SongFormat;

namespace ChartPlayer
{
    public class SongPlayer
    {
        public double PlaybackSampleRate { get; private set; } = 48000;
        public float CurrentSecond { get; private set; } = 0;
        public float SongLengthSeconds { get; private set; } = 0;
        public SongData Song { get; private set; }
        public SongInstrumentPart SongInstrumentPart { get; private set; }
        public SongKeyboardNotes SongKeyboardNotes { get; private set; }
        public SongInstrumentNotes SongInstrumentNotes { get; private set; }
        public List<SongVocal> SongVocals { get; private set; }
        public SongStructure SongStructure { get; private set; } = null;
        public bool Paused { get; set; } = false;
        public bool RetuneToEStandard { get; set; } = false;

        VorbisReader vorbisReader;
        WdlResampler resampler;
        double tuningOffsetSemitones = 0;
        float seekTime = -1;

        public SongPlayer()
        {
            resampler = new WdlResampler();

            resampler.SetMode(true, 2, false);
            resampler.SetFilterParms();
            resampler.SetFeedMode(false); // output driven
        }

        public void SetPlaybackSampleRate(double playbackRate)
        {
            this.PlaybackSampleRate = playbackRate;
        }

        public void SetSong(string songPath, SongData song, SongInstrumentPart part)
        {
            this.Song = song;
            this.SongInstrumentPart = part;

            if (part.InstrumentType == ESongInstrumentType.Keys)
            {
                using (Stream noteStream = File.OpenRead(Path.Combine(songPath, part.InstrumentName + ".json")))
                {
                    SongKeyboardNotes = JsonSerializer.Deserialize<SongKeyboardNotes>(noteStream, SongIndex.SerializerOptions);
                }

                SongInstrumentNotes = new SongInstrumentNotes();
            }
            else
            {
                using (Stream noteStream = File.OpenRead(Path.Combine(songPath, part.InstrumentName + ".json")))
                {
                    SongInstrumentNotes = JsonSerializer.Deserialize<SongInstrumentNotes>(noteStream, SongIndex.SerializerOptions);
                }
            }

            using (Stream structStream = File.OpenRead(Path.Combine(songPath, "arrangement.json")))
            {
                SongStructure = JsonSerializer.Deserialize<SongStructure>(structStream, SongIndex.SerializerOptions);
            }

            if ((part.Tuning != null) && part.Tuning.IsOffsetFromStandard())
                tuningOffsetSemitones = part.Tuning.StringSemitoneOffsets[1];

            tuningOffsetSemitones += (double)Song.A440CentsOffset / 100.0;

            SongInstrumentPart vocalPart = song.InstrumentParts.Where(p => (p.InstrumentType == ESongInstrumentType.Vocals)).FirstOrDefault();

            if (vocalPart != null)
            {
                using (Stream vocalStream = File.OpenRead(Path.Combine(songPath, vocalPart.InstrumentName + ".json")))
                {
                    SongVocals = JsonSerializer.Deserialize<List<SongVocal>>(vocalStream, SongIndex.SerializerOptions);
                }
            }
            else
            {
                SongVocals = new List<SongVocal>();
            }

            vorbisReader = new VorbisReader(Path.Combine(songPath, "song.ogg"));

            if (vorbisReader == null)
            {
                throw new InvalidOperationException("Song has no audio file");
            }

            SongLengthSeconds = (float)vorbisReader.TotalTime.TotalSeconds;
        }

        public void SeekTime(float secs)
        {
            seekTime = secs;
        }

        public int ReadSamples(Span<float> buffer)
        {
            if (seekTime != -1)
            {
                vorbisReader.TimePosition = TimeSpan.FromSeconds(seekTime);
                seekTime = -1;
            }

            if (Paused)
            {
                buffer.Clear();

                return buffer.Length;
            }

            double actualPlaybackSampleRate = PlaybackSampleRate;

            if (RetuneToEStandard && (tuningOffsetSemitones != 0))
                actualPlaybackSampleRate = PlaybackSampleRate * Math.Pow(2, (double)tuningOffsetSemitones / 12.0);

            int read = 0;

            if (actualPlaybackSampleRate == vorbisReader.SampleRate)
            {
                read = vorbisReader.ReadSamples(buffer);
            }
            else
            {
                float[] inBuffer;
                int inBufferOffset;
                int framesRequested = buffer.Length / 2;

                resampler.SetRates(vorbisReader.SampleRate, actualPlaybackSampleRate);

                int inNeeded = resampler.ResamplePrepare(framesRequested, 2, out inBuffer, out inBufferOffset);
                int inAvailable = vorbisReader.ReadSamples(inBuffer, inBufferOffset, inNeeded * 2) / 2;
                read = resampler.ResampleOut(buffer, 0, inAvailable, framesRequested, 2) * 2;
            }

            CurrentSecond = (float)vorbisReader.TimePosition.TotalSeconds;

            return read;
        }
    }
}
