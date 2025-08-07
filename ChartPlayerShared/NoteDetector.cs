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
        int[] topX = new int[5];

        public NoteDetector(int sampleRate)
        {
            pitchDetector = new(sampleRate, CorrFFTSize, zeroPad: true);
            spectrumDetector = new(sampleRate, SpecFFTSize, zeroPad: false);

            fftData = new float[Math.Max(SpecFFTSize, CorrFFTSize)];
            spectrum = new float[SpecFFTSize / 2];
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

            history.Process(CopyAudio, Math.Max(SpecFFTSize, CorrFFTSize));

            int offset = fftData.Length - CorrFFTSize;

            CurrentPitch = pitchDetector.GetPitch(new ReadOnlySpan<float>(fftData, offset, CorrFFTSize));

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

        public bool NoteDetect(params double[] frequencies)
        {
            if (frequencies.Length == 1)
            {
                if (frequencies[0] == 0)
                {
                    return (spectrum.Max() > 1);
                }

                if (CurrentPitch == 0)
                    return false;

                if (IsCloseEnough(CurrentPitch, (float)frequencies[0]))
                    return true;

                // Allow octave down
                if (IsCloseEnough(CurrentPitch / 2, (float)frequencies[0]))
                    return true;

                // Allow octave up
                if (IsCloseEnough(CurrentPitch * 2, (float)frequencies[0]))
                    return true;

                return false;
            }

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

            foreach (double freq in frequencies)
            {
                double bin = GetBin(freq);

                if (!IsPeak(bin) && !IsPeak(bin / 2) && !IsPeak(bin * 2))
                    return false;
            }

            return true;
        }
    }
}
