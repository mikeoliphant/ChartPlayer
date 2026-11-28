using System;
using System.IO;
using AudioPlugSharp;
using AudioPlugSharpUILayout;
using UILayout;
using PitchDetect;

namespace ChartPlayer
{
    /// <summary>
    /// The editor is a UILayout layout (ChartPlayerGame) drawn into the window the CLAP/VST3 adapter provides
    /// (AudioPluginUILayout), rendering on a thread of its own with vsync (the host's UI timer is too coarse).
    /// </summary>
    public class ChartPlayerPlugin : AudioPluginUILayout
    {
        public ChartPlayerSaveState ChartPlayerSaveState { get { return (SaveStateData as ChartPlayerSaveState) ?? new ChartPlayerSaveState(); } }
        public SongPlayer SongPlayer { get; private set; } = null;
        public SampleHistory<float> SampleHistory { get; private set; } = new SampleHistory<float>();
        public IMidiHandler MidiHandler { get; set; } = null;
        public float CurrentInputLevel { get; private set; } = 0;

        FloatAudioIOPort stereoInput;
        FloatAudioIOPort stereoOutput;
        AudioPluginParameter pedalParameter;
        EnvelopeFollower levelEnv;

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

            //Logger.ImmediateMode = true;

            EditorWidth = 1024;
            EditorHeight = 720;

            UseEditorRenderThread = true;

            // ChartPlayer is used like an application of its own: its keys (space to pause, ...) win over the
            // host's shortcuts while it has focus
            EditorClaimsAllKeys = true;

            SampleFormatsSupported = EAudioBitsPerSample.Bits32;

            SaveStateData = new ChartPlayerSaveState();
        }

        public override void Initialize()
        {
            base.Initialize();

            InputPorts = new AudioIOPort[]
            {
                stereoInput = new FloatAudioIOPort("Stereo Input", EAudioChannelConfiguration.Stereo),
            };

            OutputPorts = new AudioIOPort[]
            {
                stereoOutput = new FloatAudioIOPort("Stereo Output", EAudioChannelConfiguration.Stereo)
            };

            pedalParameter = new AudioPluginParameter
            {
                ID = "hihat-pedal",
                Name = "HiHat Pedal",
                MinValue = 0,
                MaxValue = 1,
                DefaultValue = 0,
                ValueFormat = "{0:0.0}"
            };

            AddParameter(pedalParameter);
            SetHiHatPedalController(4);
        }

        ChartPlayerGame game;

        // The host may resize the editor window; the layout follows (AudioPluginUILayout.SetEditorSize)
        public override bool CanResizeEditor
        {
            get { return true; }
        }

        protected override MonoGameLayout CreateEditorLayout()
        {
            Logger.Log("Create editor");

            game = new ChartPlayerGame();
            game.Plugin = this;

            return game;
        }

        protected override UIElement CreateEditorUI()
        {
            // ChartPlayerGame builds its interface when it gets its host (SetHost)
            return game.RootUIElement;
        }

        protected override void EditorOpened()
        {
            isMouseVisible = true;

            // Like the standalone app: open fullscreen if it was fullscreen when last used
            if (ChartPlayerSaveState.IsFullscreen)
                Host.SetEditorFullscreen(true);
        }

        protected override void EditorClosing()
        {
            game = null;
        }

        bool isMouseVisible = true;

        /// <summary>
        /// Cursor visibility over the editor (editor window services; applied by the host on its UI thread).
        /// </summary>
        public bool IsMouseVisible
        {
            get { return isMouseVisible; }
            set
            {
                if (value == isMouseVisible)
                    return;

                isMouseVisible = value;

                Host.SetEditorCursorVisible(value);
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
            levelEnv = new EnvelopeFollower(0, 100, (int)Host.SampleRate);
        }

        public void SetHiHatPedalController(uint controller)
        {
            AddMidiControllerMapping(pedalParameter, (uint)controller);
        }

        public void ToggleFullScreen()
        {
            // The editor window moves out of the DAW into a borderless window covering the monitor, and back (editor
            // window services; the host applies it on its UI thread and resizes the editor)
            bool fullscreen = !Host.IsEditorFullscreen;

            Host.SetEditorFullscreen(fullscreen);

            ChartPlayerSaveState.IsFullscreen = fullscreen;
        }

        public void SetSongPlayer(SongPlayer songPlayer)
        {
            this.SongPlayer = songPlayer;
        }

        public override void HandleNoteOn(int channel, int noteNumber, float velocity, int sampleOffset)
        {
            if (MidiHandler != null)
                MidiHandler.HandleNoteOn(channel, noteNumber, velocity, sampleOffset);
        }

        public override void HandlePolyPressure(int channel, int noteNumber, float pressure, int sampleOffset)
        {
            if (MidiHandler != null)
                MidiHandler.HandlePolyPressure(channel, noteNumber, pressure, sampleOffset);
        }

        public override void Process()
        {
            base.Process();

#if RELEASE
            try
            {
#endif

            var input = stereoInput.GetAudioBuffer(0);

            var left = stereoOutput.GetAudioBuffer(0);
            var right = stereoOutput.GetAudioBuffer(1);

            int currentSample = 0;
            int nextSample = 0;

            for (int i = 0; i < input.Length; i++)
            {
                float val = input[i];

                CurrentInputLevel = levelEnv.Advance(val * val);
            }

            SampleHistory.CopyFrom(input);

            DrumMidiDeviceConfiguration.CurrentMap.SetHiHatPedalValue((float)pedalParameter.ProcessValue);

            do
            {
                nextSample = Host.ProcessEvents();

                if (SongPlayer != null)
                {
                    SongPlayer.ReadSamples(left.Slice(currentSample, nextSample - currentSample), right.Slice(currentSample, nextSample - currentSample));

                    float gain = 0.25f;

                    for (; currentSample < nextSample; currentSample++)
                    {
                        left[currentSample] *= gain;// + input[i];
                        right[currentSample] *= gain;// + input[i];
                    }
                }
            }
            while (nextSample < Host.CurrentAudioBufferSize);

            if (SongPlayer == null)
            {
                left.Clear();
                right.Clear();
            }
#if RELEASE
            }
            catch (Exception ex)
            {
                Logger.Log("Process failed with: " + ex.ToString());
            }
#endif
        }
    }

    public class ChartPlayerSaveState : AudioPluginSaveState
    {
        public bool IsFullscreen { get; set; } = false;
        public SongPlayerSettings SongPlayerSettings { get; set; } = null;

        public ChartPlayerSaveState()
        {
        }
    }
}
