using System;
using System.Collections.Generic;
using System.Text;
using UILayout;
using SongFormat;

namespace ChartPlayer
{
    public class DrumUIMapping : InputMappingBase, IInputMapping
    {
        static Dictionary<DrumVoice, bool> currentHits = new Dictionary<DrumVoice, bool>();

        public DrumVoice Voice { get; private set; }

        public static void AddHit(DrumVoice voice)
        {
            currentHits[voice] = true;
        }

        public static void ClearHits()
        {
            currentHits.Clear();
        }

        public DrumUIMapping(DrumVoice voice)
        {
            this.Voice = voice;
        }

        public override bool IsDown(InputManager inputManager)
        {
            foreach (DrumVoice voice in currentHits.Keys)
            {
                if ((Voice.Articulation == EDrumArticulation.None) && (Voice.KitPiece == voice.KitPiece))
                    return true;

                if (Voice == voice)
                    return true;
            }

            return false;
        }

        public override bool WasPressed(InputManager inputManager)
        {
            return IsDown(inputManager);
        }

        public override bool WasReleased(InputManager inputManager)
        {
            return IsDown(inputManager);
        }
    }
}
