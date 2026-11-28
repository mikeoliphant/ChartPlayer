using System.IO;
using System.Reflection;

namespace ChartPlayerImageProcessor
{
    class ChartPlayerImageProcessor : ImageSheetProcessor.ImageSheetProcessor
    {
        public void RenderImages(string destPath)
        {
            BeginRenderImages(destPath);
            Render();

            EndRenderImages();
        }

        public void Render()
        {
            BeginSpriteSheetGroup("UISheet");

            string font = Path.Combine(SrcPath, "Inter_18pt-Bold.ttf");

            // The controls (frames, tabs, scroll bars, sliders) and the UI font are UILayout's default theme
            AddFont("LargeFont", font, 32);

            PushDirectory("UserInterface");

            Add("SingleWhitePixel");

            AddSvg("Play");
            AddSvg("Pause");
            AddSvg("Rewind");

            AddSvg("LevelDisplay");

            PopDirectory();

            // Drawn on the 3D note highway
            PushDirectory("NoteHighway");

            Add("VerticalFretLine");
            Add("HorizontalFretLine");

            Add("ChordOutline");

            Add("FingerOutline");

            var guitarColors = new string[] { "Red", "Yellow", "Cyan", "Orange", "Green", "Purple" };

            foreach (string color in guitarColors)
                AddWithShadow("Guitar" + color);

            AddWithShadow("GuitarDetected");

            foreach (string color in guitarColors)
                AddWithShadow("NoteTrail" + color);

            AddWithShadow("NoteTrailWhite");
            AddWithShadow("NoteTrailBlack");

            AddWithShadow("NoteHammerOn");
            AddWithShadow("NotePullOff");
            AddWithShadow("NoteMute");
            AddWithShadow("NotePalmMute");
            AddWithShadow("NoteHarmonic");
            AddWithShadow("NotePinchHarmonic");

            AddWithShadow("DrumRed");
            AddWithShadow("DrumYellow");
            AddWithShadow("DrumBlue");
            AddWithShadow("DrumGreen");

            AddWithShadow("DrumRedStick");

            AddWithShadow("CymbalYellow");
            AddWithShadow("CymbalYellowFoot");
            AddWithShadow("CymbalYellowOpen");

            AddWithShadow("CymbalGreen");

            AddWithShadow("CymbalBlue");
            AddWithShadow("CymbalBlueBell");

            AddWithShadow("CymbalChoke");

            PopDirectory();

            EndSpriteSheetGroup();
        }
    }


    class Program
    {
        static void Main(string[] args)
        {
            var processor = new ChartPlayerImageProcessor();

            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "..", "..", "..", "..");

            processor.SrcPath = Path.Combine(path, "SrcTextures");
            processor.ForceRegen = false;

            processor.RenderImages(Path.Combine(path, "ChartPlayerShared", "Content", "Textures"));
        }

    }
}
