using System.Drawing;
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

            AddFont("MainFont", "Segoe UI", FontStyle.Bold, 14);
            AddFont("LargeFont", "Segoe UI", FontStyle.Bold, 32);

            PushDirectory("UserInterface");

            Add("SingleWhitePixel");

            AddWithShadow("PopupBackground");

            Add("ButtonPressed");
            Add("ButtonUnpressed");

            Add("ScrollUpArrow");
            Add("ScrollDownArrow");

            Add("HorizontalSlider");

            Add("VerticalFretLine");
            Add("HorizontalFretLine");

            Add("ChordOutline");

            Add("FingerOutline");

            AddWithShadow("GuitarRed");
            AddWithShadow("GuitarYellow");
            AddWithShadow("GuitarCyan");
            AddWithShadow("GuitarOrange");
            AddWithShadow("GuitarGreen");
            AddWithShadow("GuitarPurple");

            AddWithShadow("GuitarDetected");

            AddWithShadow("NoteTrailRed");
            AddWithShadow("NoteTrailYellow");
            AddWithShadow("NoteTrailCyan");
            AddWithShadow("NoteTrailOrange");
            AddWithShadow("NoteTrailGreen");
            AddWithShadow("NoteTrailPurple");

            AddWithShadow("NoteTrailWhite");
            AddWithShadow("NoteTrailBlack");

            AddWithShadow("NoteHammerOn");
            AddWithShadow("NotePullOff");
            AddWithShadow("NoteMute");
            AddWithShadow("NotePalmMute");
            AddWithShadow("NoteHarmonic");
            AddWithShadow("NotePinchHarmonic");

            PopDirectory();

            EndSpriteSheetGroup();
        }
    }


    class Program
    {
        static void Main(string[] args)
        {
            var processor = new ChartPlayerImageProcessor();

            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"..\..\..\..");

            processor.SrcPath = Path.Combine(path, "SrcTextures");
            processor.ForceRegen = false;

            processor.RenderImages(Path.Combine(path, @"ChartPlayerShared\Content\Textures"));
        }

    }
}
