using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using AudioPlugSharp;
using Microsoft.Xna.Framework;
using SharpDX;
using UILayout;

namespace ChartPlayer
{
    public class ChartPlayerPlugin : AudioPluginBase
    {
        public ChartPlayerSaveState ChartPlayerSaveState { get { return (SaveStateData as ChartPlayerSaveState) ?? new ChartPlayerSaveState(); } }
        public SongPlayer SongPlayer { get; private set; } = null;
        public SampleHistory<double> SampleHistory { get; private set; } = new SampleHistory<double>();
        public MonoGameHost GameHost { get; private set; } = null;

        AudioIOPort stereoInput;
        AudioIOPort stereoOutput;
        Thread gameThread = null;

        public ChartPlayerPlugin()
        {
            Company = "Nostatic Software";
            Website = "www.nostaticsoftware.com";
            Contact = "contact@nostatic.org";
            PluginName = "ChartPlayer";
            PluginCategory = "Fx";
            PluginVersion = "1.0.0";

            // Unique 64bit ID for the plugin
            PluginID = 0x5DE6625BF8214E2F;

            Logger.ImmediateMode = true;

            HasUserInterface = true;
            EditorWidth = 1024;
            EditorHeight = 720;

            SaveStateData = new ChartPlayerSaveState();
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

            if (parentWindow == IntPtr.Zero)
            {
                RunGame();
            }
            else
            {
                gameThread = new Thread(new ThreadStart(RunGame));
                gameThread.SetApartmentState(ApartmentState.STA);
                gameThread.Start();
            }
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

            SampleHistory.SetSize((int)Host.SampleRate);
        }

        unsafe void RunGame()
        {
            Logger.Log("Starting Game");

            try
            {
                int screenWidth = (int)EditorWidth;
                int screenHeight = (int)EditorHeight;

                ChartPlayerGame game;

                Logger.Log("Create ChartPlayer Game");

                using (GameHost = new MonoGameHost(screenWidth, screenHeight, fullscreen: false))
                {
                    game = new ChartPlayerGame();
                    game.Plugin = this;

                    GameHost.IsMouseVisible = true;

                    if (parentWindow != IntPtr.Zero)
                    {
                        GameHost.Window.Position = new Microsoft.Xna.Framework.Point(0, 0);
                        GameHost.Window.IsBorderless = true;

                        SetParent(GameHost.Window.Handle, parentWindow);
                    }

                    Logger.Log("Start game");
                    GameHost.StartGame(game);
                }

                EditorWidth = (uint)GameHost.ScreenWidth;
                EditorHeight = (uint)GameHost.ScreenHeight;

                GameHost = null;
            }
            catch (Exception ex)
            {
                Logger.Log("Run game failed with: " + ex.ToString());
            }
        }

        public override void ResizeEditor(uint newWidth, uint newHeight)
        {
            base.ResizeEditor(newWidth, newHeight);

            if (GameHost != null)
            {
                GameHost.RequestResize((int)newWidth, (int)newHeight);
            }
        }

        public override void HideEditor()
        {
            base.HideEditor();

            if (GameHost != null)
            {
                GameHost.Exit();

                gameThread.Join();
            }
        }

        public void SetSongPlayer(SongPlayer songPlayer)
        {
            this.SongPlayer = songPlayer;
        }

        public override void Process()
        {
            base.Process();

            try
            {
                Host.ProcessAllEvents();

                var input = stereoInput.GetAudioBuffer(0);

                SampleHistory.CopyFrom(input);

                var left = stereoOutput.GetAudioBuffer(0);
                var right = stereoOutput.GetAudioBuffer(1);

                if (SongPlayer != null)
                {
                    SongPlayer.ReadSamples(left, right);

                    double gain = 0.25f;

                    for (int i = 0; i < Host.CurrentAudioBufferSize; i++)
                    {
                        left[i] *= gain;// + input[i];
                        right[i] *= gain;// + input[i];
                    }
                }
                else
                {
                    left.Clear();
                    right.Clear();
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Process failed with: " + ex.ToString());
            }
        }
    }

    public class ChartPlayerSaveState : AudioPluginSaveState
    {
        public SongPlayerSettings SongPlayerSettings { get; set; } = null;

        public ChartPlayerSaveState()
        {
        }
    }
}
