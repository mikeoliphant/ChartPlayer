using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Asio;
using AudioPlugSharp;
using SharpDX.MediaFoundation.DirectX;

namespace BassJam
{
    public class AudioPlugSharpHost<T> : IAudioHost where T: IAudioPlugin, IAudioPluginProcessor, IAudioPluginEditor
    {
        public T Plugin { get; private set; }

        public double SampleRate { get; private set; }
        public uint MaxAudioBufferSize { get; private set; }
        public uint CurrentAudioBufferSize { get; private set; }
        public EAudioBitsPerSample BitsPerSample { get; private set; }
        public double BPM { get; private set; }
        public long CurrentProjectSample { get; private set; }
        public bool IsPlaying { get; private set; }

        AsioDriver asioDriver;

        public AudioPlugSharpHost(T plugin)
        {
            this.Plugin = plugin;

            (plugin as IAudioPlugin).Host = this;

            plugin.Initialize();
        }

        public void SetAsioDriver(AsioDriver driver)
        {
            if (asioDriver != null)
            {
                asioDriver.Stop();
            }

            this.asioDriver = driver;

            SampleRate = driver.SampleRate;
            MaxAudioBufferSize = CurrentAudioBufferSize = (uint)driver.PreferredBufferSize();
            BitsPerSample = EAudioBitsPerSample.Bits32;            

            Plugin.InitializeProcessing();

            Plugin.SetMaxAudioBufferSize(MaxAudioBufferSize, BitsPerSample, forceCopy: true);

            asioDriver.ProcessAction = AsioProcess;

            asioDriver.Start();
        }

        unsafe void AsioProcess(IntPtr[] inputBuffers, IntPtr[] outputBuffers)
        {
            int inputCount = 0;

            for (int input = 0; input < Plugin.InputPorts.Length; input++)
            {
                AudioIOPort port = Plugin.InputPorts[input];

                for (int channel = 0; channel < port.NumChannels; channel++)
                {
                    double[] inputBuf = port.GetAudioBuffers()[channel];
                    int* asioPtr = (int*)inputBuffers[inputCount % asioDriver.NumInputChannels];    // recyle inputs if we don't have enough

                    for (int i = 0; i < CurrentAudioBufferSize; i++)
                    {
                        inputBuf[i] = (double)asioPtr[i] / (double)Int32.MaxValue;
                    }

                    inputCount++;
                }
            }

            Plugin.PreProcess();
            Plugin.Process();
            Plugin.PostProcess();

            int outputCount = 0;

            for (int output = 0; output < Plugin.OutputPorts.Length; output++)
            {
                if (outputCount >= asioDriver.NumOutputChannels)
                    break;

                AudioIOPort port = Plugin.OutputPorts[output];

                for (int channel = 0; channel < port.NumChannels; channel++)
                {
                    if (outputCount >= asioDriver.NumOutputChannels)
                        break;

                    double[] outputBuf = port.GetAudioBuffers()[channel];
                    int* asioPtr = (int*)outputBuffers[outputCount];

                    for (int i = 0; i < CurrentAudioBufferSize; i++)
                    {
                        asioPtr[i] = (int)(outputBuf[i] * Int32.MaxValue);
                    }

                    outputCount++;
                }
            }
        }

        public void BeginEdit(int parameter)
        {
        }

        public void EndEdit(int parameter)
        {
        }

        public void PerformEdit(int parameter, double normalizedValue)
        {
        }

        public void ProcessAllEvents()
        {
        }

        public int ProcessEvents()
        {
            return 0;
        }

        public void SendCC(int channel, int ccNumber, int ccValue, int sampleOffset)
        {
        }

        public void SendNoteOff(int channel, int noteNumber, float velocity, int sampleOffset)
        {
        }

        public void SendNoteOn(int channel, int noteNumber, float velocity, int sampleOffset)
        {
        }

        public void SendPolyPressure(int channel, int noteNumber, float pressure, int sampleOffset)
        {
        }
    }
}
