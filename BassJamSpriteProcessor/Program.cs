using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SpriteProcessor;

namespace BassJamSpriteProcessor
{
    class Program
    {
        static SpriteProcessor.SpriteProcessor processor;

        static void Main(string[] args)
        {
            processor = new SpriteProcessor.SpriteProcessor();

            processor.SrcPath = @"C:\Code\BassJam\SrcTextures";
            processor.ForceRegen = false;

            //processor.SetOverlayTexture("Texture.png");

            //processor.SetOverlayTexture(null);
            //RenderPromo(20);

            RenderImages(1, @"C:\Code\BassJam\BassJamShared\Content\Textures");
        }

        static void RenderImages(int defaultScale, string destPath)
        {
            processor.BeginRenderImages(defaultScale, destPath);

            Render();

            processor.EndRenderImages();
        }

        static void Render()
        {
            int initialScale = processor.TextureScale;

            processor.BeginSpriteSheetGroup("UISheet");

            processor.PushDirectory("SrcFonts");

            processor.ScaleAndParseFont("MainFont", 1, 0.1f);

            processor.ScaleAndParseFont("LargeFont", 1, 0.1f);

            processor.PopDirectory();

            processor.PushDirectory("UserInterface");

            processor.Scale("SingleWhitePixel");

            processor.TextureScale = 1;

            processor.ScaleAndShadow("PopupBackground");

            processor.Scale("PanelBackground");
            processor.Scale("PanelBackgroundDark");
            processor.Scale("PanelBackgroundDarkest");
            processor.Scale("PanelBackgroundLight");
            processor.Scale("PanelBackgroundLightest");
            processor.Scale("PanelBackgroundWhite");

            processor.Scale("VerticalFretLine");
            processor.Scale("HorizontalFretLine");


            processor.ScaleAndShadow("GuitarRed");
            processor.ScaleAndShadow("GuitarYellow");
            processor.ScaleAndShadow("GuitarCyan");
            processor.ScaleAndShadow("GuitarOrange");
            processor.ScaleAndShadow("GuitarGreen");
            processor.ScaleAndShadow("GuitarPurple");

            processor.ScaleAndShadow("GuitarDetected");

            processor.ScaleAndShadow("NoteTrailRed");
            processor.ScaleAndShadow("NoteTrailYellow");
            processor.ScaleAndShadow("NoteTrailCyan");
            processor.ScaleAndShadow("NoteTrailOrange");
            processor.ScaleAndShadow("NoteTrailGreen");
            processor.ScaleAndShadow("NoteTrailPurple");

            processor.ScaleAndShadow("NoteHammerOn");
            processor.ScaleAndShadow("NotePullOff");
            processor.ScaleAndShadow("NoteMute");

            processor.TextureScale = initialScale;

            processor.PopDirectory();

            processor.EndSpriteSheetGroup();
        }
    }
}
