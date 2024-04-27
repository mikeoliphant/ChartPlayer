using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using NVorbis;
using RubberBand;
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
        RubberBandStretcher stretcher = null;
        float[][] stretchBuf = new float[2][];
        double pitchShift = 1.0;

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

            stretcher = new RubberBandStretcher((int)playbackRate, 2, RubberBandStretcher.Options.ProcessRealTime | RubberBandStretcher.Options.WindowShort | RubberBandStretcher.Options.FormantPreserved | RubberBandStretcher.Options.PitchHighConsistency);
        }

        public void SetPlaybackSpeed(float speed)
        {
            stretcher.SetTimeRatio(1.0 / speed);
        }

        public void SetSong(string songPath, SongData song, SongInstrumentPart part)
        {
            this.Song = song;
            this.SongInstrumentPart = part;

            stretchBuf[0] = new float[1024];
            stretchBuf[1] = new float[1024];

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

            if (RetuneToEStandard && (tuningOffsetSemitones != 0))
                pitchShift = 1.0 / Math.Pow(2, (double)tuningOffsetSemitones / 12.0);

            stretcher.SetPitchScale(pitchShift);

            resampler.SetRates(vorbisReader.SampleRate, PlaybackSampleRate);

            totalSamples = (long)Math.Ceiling(PlaybackSampleRate * SongLengthSeconds);

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

            int samplesNeeded = leftChannel.Length;

            int pos = 0;

            while (samplesNeeded > 0)
            {
                int avail = stretcher.Available();

                if (avail > 0)
                {
                    int toRead = Math.Min(avail, samplesNeeded);

                    long read = stretcher.Retrieve(stretchBuf, toRead);

                    if (read != toRead)
                        throw new Exception();

                    for (int i = 0; i < toRead; i++)
                    {
                        leftChannel[pos] = stretchBuf[0][i];
                        rightChannel[pos] = stretchBuf[1][i];

                        pos++;
                    }

                    samplesNeeded -= toRead;

                    continue;
                }
                else
                {
                    long stretchSamplesRequired = stretcher.GetSamplesRequired();

                    if (stretchSamplesRequired > (totalSamples - currentPlaybackSample))
                    {
                        // We're at the end

                        leftChannel.Clear();
                        rightChannel.Clear();

                        return;
                    }

                    for (int i = 0; i < stretchSamplesRequired; i++)
                    {
                        stretchBuf[0][i] = sampleData[0][currentPlaybackSample + i];
                        stretchBuf[1][i] = sampleData[1][currentPlaybackSample + i];
                    }

                    stretcher.Process(stretchBuf, stretchSamplesRequired, final: false);

                    currentPlaybackSample += stretchSamplesRequired;
                }
            }

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

                if (PlaybackSampleRate == vorbisReader.SampleRate)
                {
                    framesOutput = vorbisReader.ReadSamples(tempBuffer, 0, samplesRequested);
                }
                else
                {
                    int inBufferOffset;
                    float[] inBuffer;

                    framesRead = resampler.ResamplePrepare(framesRequested, 2, out inBuffer, out inBufferOffset);

                    // Shouldn't happen, but sanity check
                    if (framesRead > framesLeft)
                        framesRead = (int)framesLeft;

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
