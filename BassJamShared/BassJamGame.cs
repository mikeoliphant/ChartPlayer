using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using UILayout;

namespace BassJam
{
    public class BassJamGame : MonoGameLayout
    {
        public static new BassJamGame Instance { get; private set; }

        public BassJamPlugin Plugin { get; set; }
        public Scene3D Scene3D { get; set; }

        public BassJamGame()
        {
            Instance = this;
        }

        public override void SetHost(Game host)
        {
            base.SetHost(host);

            Host.InactiveSleepTime = TimeSpan.Zero;

            LoadImageManifest("ImageManifest.xml");

            GraphicsContext.SingleWhitePixelImage = GetImage("SingleWhitePixel");

            UIFont.DefaultFont = GetFont("MainFont");
            UIFont.DefaultFont.SpriteFont.Spacing = -1;
            UIFont.DefaultFont.SpriteFont.EmptyLinePercent = 0.5f;

            TextBlock.DefaultColor = UIColor.White;

            DefaultOutlineNinePatch = GetImage("PopupBackground");

            DefaultPressedNinePatch = GetImage("PanelBackgroundLightest");
            DefaultUnpressedNinePatch = GetImage("PanelBackgroundLight");

            InputManager.AddMapping("PreviousPage", new KeyMapping(InputKey.PageUp) { DoRepeat = true });
            InputManager.AddMapping("NextPage", new KeyMapping(InputKey.PageDown) { DoRepeat = true });
            InputManager.AddMapping("NextItem", new KeyMapping(InputKey.Down) { DoRepeat = true });
            InputManager.AddMapping("PreviousItem", new KeyMapping(InputKey.Up) { DoRepeat = true });
            InputManager.AddMapping("FirstItem", new KeyMapping(InputKey.Home));
            InputManager.AddMapping("LastItem", new KeyMapping(InputKey.End));

            InputManager.AddMapping("PauseGame", new KeyMapping(InputKey.Space));

            RootUIElement = new SongPlayerInterface();
        }

        public override void Draw()
        {
            if (Scene3D != null)
            {
                Scene3D.Camera.ViewportWidth = (int)Layout.Current.Bounds.Width;
                Scene3D.Camera.ViewportHeight = (int)Layout.Current.Bounds.Height;

                Scene3D.Draw();
            }

            base.Draw();
        }
    }
}