using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using NVorbis;
using RubberBandSharp;
using SongFormat;

namespace ChartPlayer
{
    public class SongPlayer
    {
        public double PlaybackSampleRate { get; private set; } = 48000;
        public double SongSampleRate { get; private set; }
        public float CurrentSecond { get; private set; } = 0;
        public float PlaybackSpeed { get; private set; } = 1.0f;
        public float SongLengthSeconds { get; private set; } = 0;
        public SongData Song { get; private set; }
        public SongInstrumentPart SongInstrumentPart { get; private set; }
        public SongKeyboardNotes SongKeyboardNotes { get; private set; }
        public SongInstrumentNotes SongInstrumentNotes { get; private set; }
        public List<SongVocal> SongVocals { get; private set; }
        public SongStructure SongStructure { get; private set; } = null;
        public bool Paused { get; set; } = false;
        public ESongTuningMode SongTuningMode { get; set; } = ESongTuningMode.A440;
        public double TuningOffsetSemitones { get; private set; } = 0;

        VorbisReader vorbisReader;
        WdlResampler resampler;
        float seekTime = -1;
        long totalSamples;
        long currentPlaybackSample = 0;
        Thread decodeThread = null;
        float[][] sampleData = new float[2][];
        RubberBandStretcherStereo stretcher = null;
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

            try
            {
                stretcher = new RubberBandStretcherStereo((int)playbackRate,
                    RubberBandStretcher.Options.ProcessRealTime |
                    RubberBandStretcher.Options.WindowShort |
                    RubberBandStretcher.Options.FormantPreserved |
                    RubberBandStretcher.Options.PitchHighConsistency);
            }
            catch { }

            if (stretcher != null)
            {
                stretcher.SetTimeRatio(1.0 / PlaybackSpeed);
                stretcher.SetPitchScale(pitchShift);
            }
        }

        public void SetPlaybackSpeed(float speed)
        {
            PlaybackSpeed = speed;

            if (stretcher != null)
            {
                stretcher.SetTimeRatio(1.0 / speed);
            }
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
                TuningOffsetSemitones = part.Tuning.StringSemitoneOffsets[1];

            TuningOffsetSemitones += (double)Song.A440CentsOffset / 100.0;

            TuningOffsetSemitones %= 12;

            if (TuningOffsetSemitones > 6)
            {
                TuningOffsetSemitones = 12 - TuningOffsetSemitones;
            }
            else if (TuningOffsetSemitones < -6)
            {
                TuningOffsetSemitones += 12;
            }

            if (SongTuningMode > ESongTuningMode.EStandard)
            {
                TuningOffsetSemitones += (SongTuningMode - ESongTuningMode.EStandard);  // For tunings lower than E Standard
            }

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

            if ((SongTuningMode != ESongTuningMode.None) && (TuningOffsetSemitones != 0))
                pitchShift = 1.0 / Math.Pow(2, (double)TuningOffsetSemitones / 12.0);

            if (stretcher != null)
            {
                stretcher.SetPitchScale(pitchShift);
            }

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

            if ((stretcher == null) || ((pitchShift == 1.0f) && (PlaybackSpeed == 1.0f)))
            {
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
            }
            else
            {
                uint samplesNeeded = (uint)leftChannel.Length;

                int pos = 0;

                while (samplesNeeded > 0)
                {
                    int avail = stretcher.Available();

                    if (avail > 0)
                    {
                        uint toRead = (uint)Math.Min(avail, samplesNeeded);

                        uint read = stretcher.Retrieve(stretchBuf[0], stretchBuf[1], toRead);

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
                        uint stretchSamplesRequired = stretcher.GetSamplesRequired();

                        if (stretchSamplesRequired > (totalSamples - currentPlaybackSample))
                        {
                            // We're at the end

                            leftChannel.Clear();
                            rightChannel.Clear();

                            return;
                        }

                        stretcher.Process(new ReadOnlySpan<float>(sampleData[0], (int)currentPlaybackSample, (int)stretchSamplesRequired),
                            new ReadOnlySpan<float>(sampleData[1], (int)currentPlaybackSample, (int)stretchSamplesRequired), stretchSamplesRequired, isFinal: false);

                        currentPlaybackSample += stretchSamplesRequired;
                    }
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
                    if (currentOutputOffset == totalSamples)
                    {
                        // We're past the end of our buffer

                        break;
                    }

                    sampleData[0][currentOutputOffset] = tempBuffer[i];
                    sampleData[1][currentOutputOffset] = tempBuffer[i + 1];

                    currentOutputOffset++;
                }
            }
        }
    }
}
