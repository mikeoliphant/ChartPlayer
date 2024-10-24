﻿using System.IO;
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

            AddFont("MainFont", font, 16);
            AddFont("LargeFont", font, 32);

            PushDirectory("UserInterface");

            Add("SingleWhitePixel");

            AddWithShadow("PopupBackground");

            Add("ButtonPressed");
            Add("ButtonUnpressed");

            Add("Play");
            Add("Pause");

            Add("ScrollBar");
            Add("ScrollBarGutter");
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
