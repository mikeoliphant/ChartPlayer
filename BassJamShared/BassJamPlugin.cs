using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using AudioPlugSharp;
using PixelEngine;

namespace BassJam
{
    public class BassJamPlugin : AudioPluginBase
    {
        PixelEngine.XNAGame xnaGame = null;

        public SongPlayer SongPlayer { get; private set; } = null;

        AudioIOPort stereoInput;
        AudioIOPort stereoOutput;
        float[] interleavedAudio = new float[0];

        public BassJamPlugin()
        {
            Company = "Nostatic Software";
            Website = "www.nostaticsoftware.com";
            Contact = "contact@nostatic.org";
            PluginName = "Bass Jam";
            PluginCategory = "Fx";
            PluginVersion = "1.0.0";

            // Unique 64bit ID for the plugin
            PluginID = 0x5DE6625BF8214E2F;

            Logger.ImmediateMode = true;

            HasUserInterface = true;
            EditorWidth = 1024;
            EditorHeight = 720;

            FileSelector.DoNativeFileSelector = false;
        }

        public override void Initialize()
        {
            base.Initialize();

            InputPorts = new AudioIOPort[]
            {
                stereoInput = new AudioIOPort("Stereo Input", EAudioChannelConfiguration.Stereo),
            };

            OutputPorts = new AudioIOPort[]
            {
                stereoOutput = new AudioIOPort("Stereo Output", EAudioChannelConfiguration.Stereo)
            };
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        IntPtr parentWindow;

        public override void ShowEditor(IntPtr parentWindow)
        {
            Logger.Log("Show Editor");

            this.parentWindow = parentWindow;

            new Thread(new ThreadStart(RunGame)).Start();
        }

        public void Debug(String debugStr)
        {
            Logger.Log(debugStr);
        }

        public override void InitializeProcessing()
        {
            base.InitializeProcessing();

            if (SongPlayer != null)
            {
                SongPlayer.SetPlaybackSampleRate(Host.SampleRate);
            }
        }

        unsafe void RunGame()
        {
            Logger.Log("Starting Game");

            try
            {
                PixelEngine.PixGame.DebugAction = Debug;

                int screenWidth = (int)EditorWidth;
                int screenHeight = (int)EditorHeight;

                Logger.Log("Create BassJam Game");

                BassJamGame game = new BassJamGame(screenWidth, screenHeight);
                game.Plugin = this;

                using (xnaGame = new PixelEngine.XNAGame(screenWidth, screenHeight, false))
                {
                    xnaGame.IsTrialMode = false;
                    xnaGame.IsMouseVisible = true;

                    xnaGame.Window.Position = new Microsoft.Xna.Framework.Point(0, 0);
                    xnaGame.Window.IsBorderless = true;

                    SetParent(xnaGame.Window.Handle, parentWindow);

                    Logger.Log("Start XNA game");
                    xnaGame.StartGame(game);
                }

                game = null;
            }
            catch (Exception ex)
            {
                Logger.Log("Run game failed with: " + ex.ToString());
            }
        }

        public override void ResizeEditor(uint newWidth, uint newHeight)
        {
            base.ResizeEditor(newWidth, newHeight);

            if (xnaGame != null)
            {
                xnaGame.RequestResize((int)newWidth, (int)newHeight);
            }
        }

        public override void HideEditor()
        {
            base.HideEditor();

            xnaGame.Exit();
        }

        public void SetSongPlayer(SongPlayer songPlayer)
        {
            this.SongPlayer = songPlayer;
        }

        public override void SetMaxAudioBufferSize(uint maxSamples, EAudioBitsPerSample bitsPerSample, bool forceCopy)
        {
            base.SetMaxAudioBufferSize(maxSamples, bitsPerSample, forceCopy);

            Array.Resize(ref interleavedAudio, (int)maxSamples * 2);
        }

        public override void Process()
        {
            base.Process();

            try
            {
                Host.ProcessAllEvents();

                var input = stereoInput.GetAudioBuffer(0);

                var left = stereoOutput.GetAudioBuffer(0);
                var right = stereoOutput.GetAudioBuffer(1);

                if (SongPlayer != null)
                {
                    SongPlayer.ReadSamples(interleavedAudio);
                }

                int offset = 0;

                double gain = 0.25f;

                for (int i = 0; i < Host.CurrentAudioBufferSize; i++)
                {
                    left[i] = ((double)interleavedAudio[offset++] * gain) + input[i];
                    right[i] = ((double)interleavedAudio[offset++] * gain) + input[i];
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Process failed with: " + ex.ToString());
            }
        }
    }
}
