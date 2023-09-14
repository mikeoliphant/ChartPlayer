using System;
using System.IO;
using PixelEngine;

namespace BassJam
{
    public class BassJamGame : PixGame2D
    {
        public static new BassJamGame Instance { get { return PixGame2D.Instance as BassJamGame; } }

        public BassJamPlugin Plugin { get; set; }

        public BassJamGame(int screenWidth, int screenHeight) : base(screenWidth, screenHeight)
        {
        }

        public override void Initialize()
        {
            base.Initialize();

            GameHost.InactiveSleepTime = TimeSpan.Zero;
        }

        protected override void InitializeStorageManager()
        {
            string storageFolder = @"C:\tmp\BassJam";

            if (!Directory.Exists(storageFolder))
            {
                Directory.CreateDirectory(storageFolder);
            }

            StorageManager = new SimpleStorageManager(storageFolder);
        }

        public override void LoadContent()
        {
            base.LoadContent();

            ImageManifest manifest = new ImageManifest();

            try
            {
                manifest = DeserializeXml(Path.Combine(DefaultImagePath, "ImageManifest.xml"), typeof(ImageManifest)) as ImageManifest;

                LoadImageManifest(manifest);
            }
            catch (Exception ex)
            {
                //LogHandler.Instance.WriteLog("Content load failed: {0}", ex.ToString());

                Exit();
            }

            PixUI.DefaultScale = 1; // InterfaceSettings.Instance.UserInterfaceScale;

            //PixGame.Instance.SingleWhitePixelImage = PixGame.Instance.GetImage("SingleWhitePixel");

            //AddScaledFont("MainFont", "HelvetiPixel", 1);  // pentacom on bintfontmaker2
            AddFont("MainFont", GetFont("MainFont-1"));
            AddFont("LargeFont", GetFont("LargeFont-1"));

            DefaultFont = GetFont("MainFont");
            DefaultFont.Spacing = -1;
            DefaultFont.EmptyLinePercent = 0.5f;

            AddImage("PopupOutline", PixImageGen.CreateRectangleImage(4, 4, PixColor.White, PixColor.Black));

            PopupGameState.DefaultPopupNinePatch = GetImage("PopupBackground");

            PixUI.DefaultPressedNinePatch = GetImage("PanelBackgroundLightest");
            PixUI.DefaultUnpressedNinePatch = GetImage("PanelBackgroundLight");

            float DefaultPadding = PixUI.DefaultScale * 4;

            TextTouchButton.DefaultTextHorizontalPadding = DefaultPadding * 2;
            TextTouchButton.DefaultTextVerticalPadding = PixUI.DefaultScale;
            ImageTouchButton.DefaultImageHorizontalPadding = DefaultPadding * 2;
            ImageTouchButton.DefaultImageVerticalPadding = DefaultPadding * 2;

            PixGame.InputManager.AddDefaultMappings();

            PixGame.InputManager.AddMapping("PreviousPage", new KeyMapping(PixInputKey.PageUp) { DoRepeat = true });
            PixGame.InputManager.AddMapping("NextPage", new KeyMapping(PixInputKey.PageDown) { DoRepeat = true });
            PixGame.InputManager.AddMapping("NextItem", new KeyMapping(PixInputKey.Down) { DoRepeat = true });
            PixGame.InputManager.AddMapping("PreviousItem", new KeyMapping(PixInputKey.Up) { DoRepeat = true });
            PixGame.InputManager.AddMapping("FirstItem", new KeyMapping(PixInputKey.Home));
            PixGame.InputManager.AddMapping("LastItem", new KeyMapping(PixInputKey.End));


            AddGameState("SongPlayer", new SongPlayerInterface());

            SetGameState("SongPlayer");
        }
    }
}