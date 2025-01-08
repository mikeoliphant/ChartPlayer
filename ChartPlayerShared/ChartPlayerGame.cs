using System;
using UILayout;
using SongFormat;

namespace ChartPlayer
{
    public class ChartPlayerGame : MonoGameLayout
    {
        public static ChartPlayerGame Instance { get; private set; }

        public static UIColor PanelBackgroundColor = new UIColor(50, 55, 65);
        public static UIColor PanelBackgroundColorDark = PanelBackgroundColor * 0.8f;
        public static UIColor PanelBackgroundColorDarkest = PanelBackgroundColor * 0.5f;
        public static UIColor PanelBackgroundColorLight = PanelBackgroundColor * 1.5f;
        public static UIColor PanelBackgroundColorLightest = PanelBackgroundColor * 3.0f;
        public static UIColor PanelForegroundColor = UIColor.Lerp(PanelBackgroundColor, UIColor.White, 0.75f);

        public ChartPlayerPlugin Plugin { get; set; }
        public Scene3D Scene3D { get; set; }

        public ChartPlayerGame()
        {
            Instance = this;
        }

        public override void SetHost(MonoGameHost host)
        {
            base.SetHost(host);

            Host.InactiveSleepTime = TimeSpan.Zero;

            LoadImageManifest("ImageManifest.xml");

            GraphicsContext.SingleWhitePixelImage = GetImage("SingleWhitePixel");

            DefaultFont = GetFont("MainFont");
            DefaultFont.SpriteFont.EmptyLinePercent = 0.5f;

            DefaultForegroundColor = UIColor.White;

            DefaultOutlineNinePatch = GetImage("PopupBackground");

            DefaultPressedNinePatch = GetImage("ButtonPressed");
            DefaultUnpressedNinePatch = GetImage("ButtonUnpressed");

            InputManager.AddMapping("Exit", new KeyMapping(InputKey.Escape));
            InputManager.AddMapping("PreviousPage", new KeyMapping(InputKey.PageUp) { DoRepeat = true });
            InputManager.AddMapping("NextPage", new KeyMapping(InputKey.PageDown) { DoRepeat = true });
            InputManager.AddMapping("NextItem", new KeyMapping(InputKey.Down) { DoRepeat = true });
            InputManager.AddMapping("PreviousItem", new KeyMapping(InputKey.Up) { DoRepeat = true });
            InputManager.AddMapping("FirstItem", new KeyMapping(InputKey.Home));
            InputManager.AddMapping("LastItem", new KeyMapping(InputKey.End));
            InputManager.AddMapping("PlayCurrent", new KeyMapping(InputKey.Enter));
            InputManager.AddMapping("PreciseClick", new KeyMapping(InputKey.LeftShift, InputKey.RightShift));
            InputManager.AddMapping("FastForward", new KeyMapping(InputKey.Right) {  DoRepeat = true });
            InputManager.AddMapping("Rewind", new KeyMapping(InputKey.Left) {  DoRepeat = true });
            InputManager.AddMapping("ToggleFavorite", new KeyMapping(InputKey.D8) { Modifier = InputKey.LeftShift });
            InputManager.AddMapping("ToggleFavorite", new KeyMapping(InputKey.D8) { Modifier = InputKey.RightShift });
            InputManager.AddMapping("ToggleFavorite", new KeyMapping(InputKey.Multiply));

            InputManager.AddMapping("PreviousPage", new DrumUIMapping(new DrumVoice(EDrumKitPiece.Tom1, EDrumArticulation.DrumHead)));
            InputManager.AddMapping("NextPage", new DrumUIMapping(new DrumVoice(EDrumKitPiece.Tom2, EDrumArticulation.DrumHead)));

            InputManager.AddMapping("ToggleFullscreen", new KeyMapping(InputKey.Enter) { Modifier = InputKey.LeftAlt });
            InputManager.AddMapping("ToggleFullscreen", new KeyMapping(InputKey.Enter) { Modifier = InputKey.RightAlt });

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

        public override void Update(float secondsElapsed)
        {
            base.Update(secondsElapsed);

            if (InputManager.WasClicked("ToggleFullscreen", this))
            {
                Plugin.ToggleFullScreen();
            }
        }

        public override void Exiting()
        {
            SongPlayerInterface.Instance.Exit();

            base.Exiting();
        }
    }
}