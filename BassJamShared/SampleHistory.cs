using System;
using System.Collections.Generic;
using System.Text;

namespace BassJamShared
{
    public class SampleHistory<T>
    {
        public int CurrentOffset { get; private set; } = 0;
        public int Size { get { return data.Length; } }

        T[] data = new T[0];

        public SampleHistory()
        {
        }

        public void SetSize(int size)
        {
            Array.Resize(ref data, size);
        }
    }
}
