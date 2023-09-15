using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NVorbis;
using SongFormat;

namespace BassJam
{
    public class SongPlayer
    {
        public double PlaybackSampleRate { get; private set; } = 48000;
        public double CurrentSecond { get; private set; } = 0;
        public SongData Song { get; private set; }
        public SongInstrumentPart SongInstrumentPart { get; private set; }
        public SongInstrumentNotes SongInstrumentNotes { get; private set; }
        public List<SongVocal> SongVocals { get; private set; }
        public SongStructure SongStructure { get; private set; } = null;

        VorbisReader vorbisReader;
        WdlResampler resampler;
        double tuningOffsetSemitones = 0;
        double actualPlaybackSampleRate = 48000;

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

            using (Stream noteStream = File.OpenRead(Path.Combine(songPath, part.InstrumentName + ".json")))
            {
                SongInstrumentNotes = JsonSerializer.Deserialize<SongInstrumentNotes>(noteStream);
            }

            using (Stream structStream = File.OpenRead(Path.Combine(songPath, "arrangement.json")))
            {
                SongStructure = JsonSerializer.Deserialize<SongStructure>(structStream);
            }

            if (part.Tuning.IsOffsetFromStandard())
                tuningOffsetSemitones = part.Tuning.StringSemitoneOffsets[1];

            tuningOffsetSemitones += (double)Song.A440CentsOffset / 100.0;

            if (tuningOffsetSemitones == 0)
                actualPlaybackSampleRate = PlaybackSampleRate;
            else
                actualPlaybackSampleRate = PlaybackSampleRate * Math.Pow(2, (double)tuningOffsetSemitones / 12.0);

            SongInstrumentPart vocalPart = song.InstrumentParts.Where(p => (p.InstrumentType == ESongInstrumentType.Vocals)).FirstOrDefault();

            if (vocalPart != null)
            {
                using (Stream vocalStream = File.OpenRead(Path.Combine(songPath, vocalPart.InstrumentName + ".json")))
                {
                    SongVocals = JsonSerializer.Deserialize<List<SongVocal>>(vocalStream);
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

            resampler.SetRates(vorbisReader.SampleRate, actualPlaybackSampleRate);
        }

        public void SeekTime(float secs)
        {
            vorbisReader.TimePosition = TimeSpan.FromSeconds(secs);
        }

        public int ReadSamples(float[] buffer)
        {
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

                int inNeeded = resampler.ResamplePrepare(framesRequested, 2, out inBuffer, out inBufferOffset);
                int inAvailable = vorbisReader.ReadSamples(inBuffer, inBufferOffset, inNeeded * 2) / 2;
                read = resampler.ResampleOut(buffer, 0, inAvailable, framesRequested, 2) * 2;
            }

            CurrentSecond = vorbisReader.TimePosition.TotalSeconds;

            return read;
        }
    }
}
