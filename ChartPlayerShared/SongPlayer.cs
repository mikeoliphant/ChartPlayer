using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Microsoft.VisualBasic.Logging;
using NVorbis;
using SongFormat;

namespace ChartPlayer
{
    public class SongPlayer
    {
        public double PlaybackSampleRate { get; private set; } = 48000;
        public double SongSampleRate { get; private set; }
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
        long totalSamples;
        long currentPlaybackSample = 0;
        Thread decodeThread = null;
        float[][] sampleData = new float[2][];
        double actualPlaybackSampleRate;

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

            actualPlaybackSampleRate = PlaybackSampleRate;

            if (RetuneToEStandard && (tuningOffsetSemitones != 0))
                actualPlaybackSampleRate = PlaybackSampleRate * Math.Pow(2, (double)tuningOffsetSemitones / 12.0);

            resampler.SetRates(vorbisReader.SampleRate, actualPlaybackSampleRate);

            totalSamples = (long)Math.Ceiling(actualPlaybackSampleRate * SongLengthSeconds);

            sampleData[0] = new float[totalSamples];
            sampleData[1] = new float[totalSamples];

            decodeThread = new Thread(new ThreadStart(RunDecode));
            decodeThread.Start();
        }

        public void SeekTime(float secs)
        {
            seekTime = secs;
            CurrentSecond = seekTime;
        }

        public void ReadSamples(Span<double> leftChannel, Span<double> rightChannel)
        {
            if (seekTime != -1)
            {
                currentPlaybackSample = (int)((seekTime / SongLengthSeconds) * totalSamples);

                seekTime = -1;
            }

            if (Paused)
            {
                leftChannel.Clear();
                rightChannel.Clear();

                return;
            }

            int samples = (int)Math.Min(leftChannel.Length, totalSamples - currentPlaybackSample);

            for (int i = 0; i < samples; i++)
            {
                leftChannel[i] = sampleData[0][currentPlaybackSample + i];
                rightChannel[i] = sampleData[1][currentPlaybackSample + i];
            }

            for (int i = samples; i < leftChannel.Length; i++)
            {
                leftChannel[i] = 0;
                rightChannel[i] = 0;
            }

            currentPlaybackSample += samples;

            CurrentSecond = ((float)currentPlaybackSample / (float)totalSamples) * SongLengthSeconds;
        }

        void RunDecode()
        {
            long currentOutputOffset = 0;

            float[] tempBuffer = new float[1024];

            long framesLeft = vorbisReader.TotalSamples;

            while (framesLeft > 0)
            {
                int samplesRequested = (int)Math.Min(framesLeft * 2, tempBuffer.Length);
                int framesRequested = samplesRequested / 2;

                int framesRead = framesRequested;
                int framesOutput;

                if (actualPlaybackSampleRate == vorbisReader.SampleRate)
                {
                    framesOutput = vorbisReader.ReadSamples(tempBuffer, 0, samplesRequested);
                }
                else
                {
                    int inBufferOffset;
                    float[] inBuffer;

                    framesRead = resampler.ResamplePrepare(framesRequested, 2, out inBuffer, out inBufferOffset);
                    int inAvailable = vorbisReader.ReadSamples(inBuffer, inBufferOffset, framesRead * 2) / 2;

                    framesOutput = resampler.ResampleOut(tempBuffer, 0, inAvailable, framesRequested, 2) * 2;
                }

                // Shouldn't happen, but sanity check
                if (framesRead == 0)
                    break;

                framesLeft -= framesRead;

                for (int i = 0; i < framesOutput; i += 2)
                {
                    sampleData[0][currentOutputOffset] = tempBuffer[i];
                    sampleData[1][currentOutputOffset] = tempBuffer[i + 1];

                    currentOutputOffset++;
                }
            }
        }
    }
}
