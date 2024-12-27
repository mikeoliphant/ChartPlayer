using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NVorbis;

namespace ChartPlayer
{
    public class VorbisMixer
    {
        public TimeSpan TotalTime { get; private set; }
        public long TotalSamples { get; private set; }
        public int SampleRate { get { return readers[0].SampleRate; } }

        List<VorbisReader> readers = new List<VorbisReader>();
        float[] mixBuf = new float[0];

        public VorbisMixer(string oggPath)
        {
            if (oggPath.EndsWith(".ogg", StringComparison.InvariantCultureIgnoreCase))
            {
                readers.Add(new VorbisReader(oggPath));

                TotalTime = readers[0].TotalTime;
            }
            else
            {
                foreach (string ogg in Directory.GetFiles(oggPath, "*.ogg"))
                {
                    readers.Add(new VorbisReader(ogg));
                }
            }

            TotalTime = readers.Max(r => r.TotalTime);
            TotalSamples = readers.Max(r => r.TotalSamples);
        }

        public int ReadSamples(float[] buffer, int offset, int count)
        {
            if (readers.Count == 1)
            {
                return readers[0].ReadSamples(buffer, offset, count);
            }

            int read = 0;

            if (mixBuf.Length != count)
                Array.Resize(ref mixBuf, count);

            Array.Clear(buffer, offset, count);

            foreach (var reader in readers)
            {
                int toMix = reader.ReadSamples(mixBuf, 0, count);

                for (int i = 0; i < toMix; i++)
                {
                    buffer[i + offset] += mixBuf[i] * 0.6f;
                }

                read = Math.Max(read, toMix);
            }

            return read;
        }
    }
}
