using System;
using System.Drawing;
using System.IO;
using Microsoft.Xna.Framework;
using UILayout;

namespace BassJam
{
    public class BassJamGame : MonoGameLayout
    {
        public static new BassJamGame Instance { get; private set; }

        public int ScreenWidth { get; private set; }
        public int ScreenHeight { get; private set; }

        public BassJamPlugin Plugin { get; set; }

        public BassJamGame(Game host)
            : base(host)
        {
            Instance = this;
            //}

            //public override void Initialize()
            //{
            //    base.Initialize();

            //    GameHost.InactiveSleepTime = TimeSpan.Zero;
            //}

            //public override void LoadContent()
            //{
            //    base.LoadContent();
            LoadImageManifest("ImageManifest.xml");

            //PixUI.DefaultScale = 1; // InterfaceSettings.Instance.UserInterfaceScale;

            //PixGame.Instance.SingleWhitePixelImage = PixGame.Instance.GetImage("SingleWhitePixel");

            AddFont("MainFont", GetFont("MainFont-1"));
            AddFont("LargeFont", GetFont("LargeFont-1"));

            UIFont.DefaultFont = GetFont("MainFont");
            UIFont.DefaultFont.SpriteFont.Spacing = -1;
            UIFont.DefaultFont.SpriteFont.EmptyLinePercent = 0.5f;

            //AddImage("PopupOutline", UIImageGen.CreateRectangleImage(4, 4, UIColor.White, UIColor.Black));

            DefaultOutlineNinePatch = GetImage("PopupBackground");

            DefaultPressedNinePatch = GetImage("PanelBackgroundLightest");
            DefaultUnpressedNinePatch = GetImage("PanelBackgroundLight");

            float DefaultPadding = 4;

            //TextTouchButton.DefaultTextHorizontalPadding = DefaultPadding * 2;
            //TextTouchButton.DefaultTextVerticalPadding = PixUI.DefaultScale;
            //ImageTouchButton.DefaultImageHorizontalPadding = DefaultPadding * 2;
            //ImageTouchButton.DefaultImageVerticalPadding = DefaultPadding * 2;

            //PixGame.InputManager.AddDefaultMappings();

            InputManager.AddMapping("PreviousPage", new KeyMapping(InputKey.PageUp) { DoRepeat = true });
            InputManager.AddMapping("NextPage", new KeyMapping(InputKey.PageDown) { DoRepeat = true });
            InputManager.AddMapping("NextItem", new KeyMapping(InputKey.Down) { DoRepeat = true });
            InputManager.AddMapping("PreviousItem", new KeyMapping(InputKey.Up) { DoRepeat = true });
            InputManager.AddMapping("FirstItem", new KeyMapping(InputKey.Home));
            InputManager.AddMapping("LastItem", new KeyMapping(InputKey.End));

            InputManager.AddMapping("PauseGame", new KeyMapping(InputKey.Space));

            RootUIElement = new SongPlayerInterface();
            //AddGameState("SongPlayer", new SongPlayerInterface());

            //SetGameState("SongPlayer");
        }

        //public override void ResizeScreen(int newWidth, int newHeight, bool gameSizeOnly)
        //{
        //    base.ResizeScreen(newWidth, newHeight, gameSizeOnly);

        //    SongPlayerInterface.Instance.ResizeScreen();
        //}
    }
}