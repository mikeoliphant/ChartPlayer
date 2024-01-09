using System.Drawing;

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

            Add("PanelBackground");
            Add("PanelBackgroundDark");
            Add("PanelBackgroundDarkest");
            Add("PanelBackgroundLight");
            Add("PanelBackgroundLightest");
            Add("PanelBackgroundWhite");

            Add("VerticalFretLine");
            Add("HorizontalFretLine");

            Add("ChordOutline");

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

            PopDirectory();

            EndSpriteSheetGroup();
        }
    }


    class Program
    {
        static void Main(string[] args)
        {
            var processor = new ChartPlayerImageProcessor();

            processor.SrcPath = @"C:\Code\ChartPlayer\SrcTextures";
            processor.ForceRegen = false;

            processor.RenderImages(@"C:\Code\ChartPlayer\ChartPlayerShared\Content\Textures");
        }

    }
}
