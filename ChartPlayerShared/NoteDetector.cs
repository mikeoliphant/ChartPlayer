using System;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UILayout;

namespace ChartPlayer
{
    public class NoteDetector
    {
        public double MaxFrequency { get; set; }

        const int FFTSize = 4096;
        Complex[] fftData = new Complex[FFTSize];
        float[] fftOutput = new float[FFTSize / 2];
        int msPerPass = 50;

        List<(float Power, int Bin)> topX = null;
        Stopwatch stopwatch = new Stopwatch();
        bool stop = false;

        public void Run()
        {
            while (!stop)
            {
                stopwatch.Reset();
                stopwatch.Start();

                UpdateFFT();

                long elapsedMS = stopwatch.ElapsedMilliseconds;

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

        float GetPower(double frequency)
        {
            double bin = GetBin(frequency);

            int low = (int)Math.Floor(bin);

            double partial = bin - low;

            return (float)MathUtil.Lerp(fftOutput[low], fftOutput[low + 1], partial);
        }

        double GetBin(double frequency)
        {
            return (fftData.Length * (frequency / ChartPlayerGame.Instance.Plugin.Host.SampleRate));
        }

        void ConvertToComplex(ReadOnlySpan<float> samples, int offset)
        {
            for (int pos = 0; pos < samples.Length; pos++)
            {
                fftData[pos + offset].X = (float)samples[pos] * (float)FastFourierTransform.HammingWindow(pos + offset, fftData.Length);
                fftData[pos + offset].Y = 0;
            }
        }

        void UpdateFFT()
        {
            SampleHistory<float> history = ChartPlayerGame.Instance.Plugin.SampleHistory;

            history.Process(ConvertToComplex, fftData.Length);

            FastFourierTransform.FFT(true, (int)Math.Log(fftData.Length, 2.0), fftData);

            for (int i = 0; i < fftData.Length / 2; i++)
            {
                float fft = Math.Abs(fftData[i].X + fftData[i].Y);
                float fftMirror = Math.Abs(fftData[fftData.Length - i - 1].X + fftData[fftData.Length - i - 1].Y);

                fftOutput[i] = (fft + fftMirror) * (0.5f + (i / (fftData.Length * 2)));
            }

            int maxFrequencyBin = (int)GetBin(MaxFrequency);  // Max note frequency

            topX = fftOutput.Take(maxFrequencyBin).Select((Power, Bin) => (Power, Bin)).OrderByDescending(x => x.Power).Take(10).ToList();
            //{
            //    topX.Add((top.Power + fftOutput[top.Bin * 2], top.Bin));
            //}
        }

        public bool NoteDetect(params double[] frequencies)
        {
            if (topX == null)
                return false;

            int numInTop = 0;

            foreach (double freq in frequencies)
            {
                int bin = (int)GetBin(freq);

                if (topX.Take(frequencies.Length * 2).Where(x => (x.Bin == bin) || (x.Bin == (bin * 2)) || (x.Bin == (bin * 3))).Any())
                {
                    numInTop++;
                }
            }

            float totPower = 0;

            foreach (var freq in topX)
            {
                totPower += freq.Power;
            }

            if (totPower > .001)
            {
                if (frequencies.Length == 1)
                {
                    if (numInTop == 0)
                    {

                    }

                    return (numInTop == 1);
                }

                return (numInTop >= frequencies.Length - 1);
            }

            return false;
        }
    }
}
