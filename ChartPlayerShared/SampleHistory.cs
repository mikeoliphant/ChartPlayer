using System;
using System.Collections.Generic;
using System.Text;

namespace ChartPlayer
{
    public class SampleHistory<T>
    {
        public delegate void SampleProcessDelegate(ReadOnlySpan<T> data, int offset);

        public int CurrentOffset { get; private set; } = 0;
        public int Size { get { return data.Length; } }

        T[] data;
        Memory<T> dataPtr;

        public SampleHistory()
        {
            data = new T[0];
            dataPtr = data;
        }

        public void SetSize(int size)
        {
            if (data.Length != size)
            {
                Array.Resize(ref data, size);
                dataPtr = data;
            }
        }

        public void CopyFrom(ReadOnlySpan<T> source)
        {
            int left = source.Length;

            do
            {
                int toCopy = Math.Min(left, data.Length - CurrentOffset);

                source.Slice(0, toCopy).CopyTo(dataPtr.Slice(CurrentOffset, toCopy).Span);

                CurrentOffset = (CurrentOffset + toCopy) % data.Length;
                left -= toCopy;
            }
            while(left > 0);
        }

        public void Process(SampleProcessDelegate processDelegate, int numSamples)
        {
            int startCurrentOffset = CurrentOffset;

            int offset = CurrentOffset - numSamples;

            if (offset < 0)
            {
                offset += data.Length;
            }

            int processed = 0;

            do
            {
                int toProcess = Math.Min(numSamples, data.Length - offset);

                processDelegate(dataPtr.Slice(offset, toProcess).Span, processed);

                offset = (offset + toProcess) % data.Length;
                numSamples -= toProcess;
                processed += toProcess;
            }
            while (numSamples > 0);

            int endCurrentOffset = CurrentOffset;
        
        }
    }
}
