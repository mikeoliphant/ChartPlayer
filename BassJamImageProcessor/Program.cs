using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Text;
using ImageSheetProcessor;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing.Text;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using static System.Formats.Asn1.AsnWriter;
using UILayout;

namespace BassJamImageProcessor
{
    class BassJamImageProcessor : ImageSheetProcessor.ImageSheetProcessor
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

            PushDirectory("SrcFonts");

            AddFont("MainFont", "Segoe UI", 14);
            AddFont("LargeFont", "Segoe UI", FontStyle.Bold, 32);

            //ScaleAndParseFont("MainFont", 1, 0.1f);

            //ScaleAndParseFont("LargeFont", 1, 0.1f);

            PopDirectory();

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
            var processor = new BassJamImageProcessor();

            processor.SrcPath = @"C:\Code\BassJam\SrcTextures";
            processor.ForceRegen = false;

            processor.RenderImages(@"C:\Code\BassJam\BassJamShared\Content\Textures");
        }

    }
}
