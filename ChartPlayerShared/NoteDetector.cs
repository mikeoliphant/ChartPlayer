using AudioPlugSharp;
using PitchDetect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace ChartPlayer
{
    public class NoteDetector
    {
        public double MaxFrequency { get; set; }
        public float CurrentPitch { get; private set; } = 0;

        const int SpecFFTSize = 8192;
        const int CorrFFTSize = 4096;

        PitchDetector pitchDetector;
        PitchDetector spectrumDetector;
        float[] fftData;
        float[] spectrum;
        int msPerPass = 50;

        Stopwatch stopwatch = new Stopwatch();
        bool stop = false;
        float validPitchRatio = MathF.Pow(2, 0.5f / 12.0f); // half a semitone
        int[] topX = new int[6];
        (float Freq, float Corr)[] peaks = new (float Freq, float Corr)[16];
        SampleHistory<float>.SampleProcessDelegate copyDelegate;

        public NoteDetector(int sampleRate)
        {
            pitchDetector = new(sampleRate, CorrFFTSize, zeroPad: true);
            spectrumDetector = new(sampleRate, SpecFFTSize, zeroPad: false);

            fftData = new float[Math.Max(SpecFFTSize, CorrFFTSize)];
            spectrum = new float[SpecFFTSize / 2];

            copyDelegate = CopyAudio;
        }

        public void Run()
        {
            while (!stop)
            {
                stopwatch.Reset();
                stopwatch.Start();

                UpdateFFT();

                double elapsedMS = stopwatch.Elapsed.TotalMilliseconds;

                if (elapsedMS < msPerPass)
                {
                    Thread.Sleep(msPerPass - (int)elapsedMS);
                }
            }
        }

        public void Stop()
        {
            stop = true;
        }

        double GetBin(double frequency)
        {
            return (fftData.Length * (frequency / ChartPlayerGame.Instance.Plugin.Host.SampleRate));
        }

        void CopyAudio(ReadOnlySpan<float> samples, int offset)
        {
            for (int pos = 0; pos < samples.Length; pos++)
            {
                fftData[pos + offset] = samples[pos];
            }
        }

        void UpdateFFT()
        {
            SampleHistory<float> history = ChartPlayerGame.Instance.Plugin.SampleHistory;

            history.Process(copyDelegate, Math.Max(SpecFFTSize, CorrFFTSize));

            int offset = fftData.Length - CorrFFTSize;

            float newPitch = 0;

            int numPeaks = pitchDetector.GetPitchPeaks(new ReadOnlySpan<float>(fftData, offset, CorrFFTSize), peaks);

            if (numPeaks > 0)
            {
                if (CurrentPitch != 0)
                {
                    for (int i = 0; i < numPeaks; i++)
                    {
                        if (Math.Abs(NoteUtil.GetSemitoneDifference(CurrentPitch, peaks[i].Freq)) < 0.5)
                        {
                            newPitch = peaks[i].Freq;

                            break;
                        }
                    }
                }

                if (newPitch == 0)
                {
                    float maxPeak = peaks.Select(p => p.Corr).Max();

                    newPitch = peaks.Where(p => p.Corr > (maxPeak * 0.25f)).FirstOrDefault().Freq;
                }
            }

            CurrentPitch = newPitch;

            offset = fftData.Length - SpecFFTSize;

            spectrumDetector.GetSpectrum(new ReadOnlySpan<float>(fftData, offset, SpecFFTSize), spectrum);
        }

        bool IsPeak(double bin)
        {
            return topX.Contains((int)bin) || topX.Contains((int)(bin - 0.4)) || topX.Contains((int)(bin + 0.4));
        }

        bool IsCloseEnough(float pitch, float desiredPitch)
        {
            float ratio = desiredPitch / pitch;

            if (ratio < 1)
                ratio = 1.0f / ratio;

            return ratio < validPitchRatio;
        }

        public bool NoteDetect(double frequency)
        {
                if (frequency == 0)
                {
                    return (spectrum.Max() > 1);
                }

                if (CurrentPitch == 0)
                    return false;

                if (IsCloseEnough(CurrentPitch, (float)frequency))
                    return true;

                // Allow octave down
                if (IsCloseEnough(CurrentPitch / 2, (float)frequency))
                    return true;

                // Allow octave up
                if (IsCloseEnough(CurrentPitch * 2, (float)frequency))
                    return true;

                return false;
        }

        public bool NoteDetect(double[] frequencies, int numFreqs)
        {
            double max = spectrum.Max();

            if (max < 1)
                return false;

            double min = max * 0.1;

            int topPos = 0;

            Array.Clear(topX);

            for (int bin = 1; bin < (spectrum.Length - 1); bin++)
            {
                if (spectrum[bin] < min)
                    continue;

                if ((spectrum[bin] > spectrum[bin - 1]) && (spectrum[bin] > spectrum[bin + 1]))
                {
                    topX[topPos++] = bin;

                    if (topPos == topX.Length)
                        break;
                }
            }

            for(int pos = 0; pos < numFreqs; pos++)
            {
                double bin = GetBin(frequencies[pos]);

                if (!IsPeak(bin) && !IsPeak(bin / 2) && !IsPeak(bin / 4) && !IsPeak(bin * 2))
                    return false;
            }

            return true;
        }
    }
}
