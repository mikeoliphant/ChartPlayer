﻿using System;
using Microsoft.Xna.Framework;
using UILayout;

namespace ChartPlayer
{
    public class ChartPlayerGame : MonoGameLayout
    {
        public static new ChartPlayerGame Instance { get; private set; }

        public ChartPlayerPlugin Plugin { get; set; }
        public Scene3D Scene3D { get; set; }

        public ChartPlayerGame()
        {
            Instance = this;
        }

        public override void SetHost(Game host)
        {
            base.SetHost(host);

            Host.InactiveSleepTime = TimeSpan.Zero;

            LoadImageManifest("ImageManifest.xml");

            GraphicsContext.SingleWhitePixelImage = GetImage("SingleWhitePixel");

            DefaultFont = GetFont("MainFont");
            DefaultFont.SpriteFont.Spacing = -1;
            DefaultFont.SpriteFont.EmptyLinePercent = 0.5f;

            DefaultForegroundColor = UIColor.White;

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