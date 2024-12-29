using System;
using System.Collections.Generic;
using System.IO;
using UILayout;
using SongFormat;
using System.Numerics;

namespace ChartPlayer
{
    public class MidiNoteDisplay : Dock
    {
        HorizontalStack stack;
        UIElement overlay;

        public override List<UIElement> Children
        {
            get
            {
                return stack.Children;
            }
        }

        public MidiNoteDisplay()
        {
            stack = new HorizontalStack { HorizontalAlignment = EHorizontalAlignment.Stretch };
            base.Children.Add(stack);

            overlay = new UIElement { HorizontalAlignment = EHorizontalAlignment.Stretch, VerticalAlignment = EVerticalAlignment.Stretch, BackgroundColor = UIColor.Transparent };
            base.Children.Add(overlay);
        }

        public void Flash()
        {
            overlay.BackgroundColor = UIColor.White;
        }

        protected override void DrawContents()
        {
            overlay.BackgroundColor = UIColor.Lerp(overlay.BackgroundColor, UIColor.Transparent, 0.1f);

            base.DrawContents();

            BackgroundColor = UIColor.Lerp(BackgroundColor, UIColor.Transparent, 0.5f);
        }
    }

    public class FileSelectionSwipeList : SwipeList
    {
        public Action<string> PathSelectAction { get; set; }

        string[] paths;

        public FileSelectionSwipeList(string folder, string pattern)
        {
            paths = Directory.GetFiles(folder, pattern);

            Items = new List<string>();

            foreach (string path in paths)
            {
                Items.Add(Path.GetFileNameWithoutExtension(path));
            }

            SelectAction = FileSelected;
        }

        void FileSelected(int index)
        {
            if (PathSelectAction != null)
            {
                PathSelectAction(paths[index]);
            }
        }
    }

    public class MidiConfigSelectionDialog : InputDialog
    {
        public MidiConfigSelectionDialog()
            : base(Layout.Current.DefaultOutlineNinePatch)
        {
            FileSelectionSwipeList fileSelect = new FileSelectionSwipeList(Path.Combine(MidiEditor.ConfigPath, "MidiMaps"), "*.xml");
            fileSelect.HorizontalAlignment = EHorizontalAlignment.Stretch;
            fileSelect.DesiredHeight = 200;
            fileSelect.BackgroundColor = UIColor.Black;
            fileSelect.PathSelectAction = MidiConfigSelected;
            SetContents(fileSelect);

            AddInput(new DialogInput { Text = "Load Custom", Action = MidiEditor.Instance.LoadCustomMidiMap, CloseOnInput = true });
            AddInput(new DialogInput { Text = "Cancel", CloseOnInput = true });
        }

        void MidiConfigSelected(string path)
        {
            MidiEditor.Instance.UpdateMidiConfig(path);

            CloseAction();
        }
    }

    public class MidiEditor : Dock
    {
        public static string ConfigPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ChartPlayer");
        public static MidiEditor Instance { get; private set; }

        HorizontalStack midiNoteStack;
        FileSelector midiConfigSelector;
        UIElementWrapper configWrapper;
        Dictionary<int, MidiNoteDisplay> midiNoteDisplays = new Dictionary<int, MidiNoteDisplay>();
        int hiHatPedalValue;
        int snarePositionValue;
        int midiNoteNumber = 0;
        int midiNoteVelocity;
        TextBlock midiEventText;
        HiHatPedalDisplay hiHatPedalDisplay;

        public MidiEditor()
        {
            Instance = this;

            HorizontalAlignment = EHorizontalAlignment.Stretch;
            VerticalAlignment = EVerticalAlignment.Stretch;

            Padding = 20;

            BackgroundColor = UIColor.Black;

            VerticalStack mainVStack = new VerticalStack { HorizontalAlignment = EHorizontalAlignment.Stretch, VerticalAlignment = EVerticalAlignment.Center };
            Children.Add(mainVStack);

            string midiUserPath = Path.Combine(ConfigPath, "MidiMaps");

            if (!Directory.Exists(midiUserPath))
                Directory.CreateDirectory(midiUserPath);

            //midiConfigSelector = new FileSelector("Midi Configuration", canCreateFolders: true);
            //midiConfigSelector.AllowedExtensions = new string[] { ".xml" };
            //midiConfigSelector.SetRootPath(midiUserPath);
            //midiConfigSelector.FileAction = UpdateMidiConfig;

            //PixGame.Instance.AddGameState("MidiConfigSelector", midiConfigSelector);

            HorizontalStack mainStack = new HorizontalStack { HorizontalAlignment = EHorizontalAlignment.Stretch, VerticalAlignment = EVerticalAlignment.Stretch, ChildSpacing = 10 };
            mainVStack.Children.Add(mainStack);

            VerticalStack leftStack = new VerticalStack
            {
                DesiredWidth = 250,
                ChildSpacing = 5
            };
            mainStack.Children.Add(leftStack);

            NinePatchWrapper configPanel = new NinePatchWrapper(Layout.Current.DefaultOutlineNinePatch)
            {
                HorizontalAlignment = EHorizontalAlignment.Stretch
            };
            leftStack.Children.Add(configPanel);

            VerticalStack configStack = new VerticalStack { ChildSpacing = 5, HorizontalAlignment = EHorizontalAlignment.Stretch };

            configPanel.Child = configStack;

            configWrapper = new UIElementWrapper() { HorizontalAlignment = EHorizontalAlignment.Stretch };
            configStack.Children.Add(configWrapper);

            configStack.Children.Add(new TextButton("Save Midi Config") { HorizontalAlignment = EHorizontalAlignment.Stretch });

            UIElementWrapper noteWrapper = new UIElementWrapper { HorizontalAlignment = EHorizontalAlignment.Stretch };
            mainStack.Children.Add(noteWrapper);

            midiNoteStack = new HorizontalStack() { HorizontalAlignment = EHorizontalAlignment.Center, VerticalAlignment = EVerticalAlignment.Top, ChildSpacing = 20 };
            noteWrapper.Child = midiNoteStack;

            NinePatchWrapper noteDisplay = new NinePatchWrapper(Layout.Current.DefaultOutlineNinePatch) { HorizontalAlignment = EHorizontalAlignment.Stretch };
            leftStack.Children.Add(noteDisplay);
            midiEventText = new TextBlock("Last Note: None");
            noteDisplay.Child = midiEventText;

            hiHatPedalDisplay = new HiHatPedalDisplay() { HorizontalAlignment = EHorizontalAlignment.Stretch };
            leftStack.Children.Add(hiHatPedalDisplay);

            //snarePositionDisplay = new PositionalSensingDisplay() { HorizontalAlignment = EHorizontalAlignment.Stretch };
            //leftStack.Children.Add(snarePositionDisplay);

            UpdateMapDisplay();
        }

        public void UpdateMidiConfig(string path)
        {
            SetMidiConfig(path);
        }

        void SaveMidiConfig(string path)
        {            
            //MixerSettings.Instance.MidiConfig = path;

            //configWrapper.Child = new OptionButtonStack("Config:", Path.GetFileNameWithoutExtension(MixerSettings.Instance.MidiConfig), "SelectMidiConfig");

            //DrumAudioHost.Instance.DrumMidiConfig.SaveXml(MixerSettings.Instance.MidiConfig);

            //MixerSettings.SaveXml(Path.Combine(DrumAudioHost.UserDataFolder, "MixerSettings.xml"));

            //PixGame.Instance.UserInterface.NeedLayoutUpdate = true;
        }

        public void SetMidiConfig(string path)
        {
            DrumMidiDeviceConfiguration.CurrentMap = DrumMidiDeviceConfiguration.LoadFromXml(path);

            UpdateMapDisplay();
            //snarePositionDisplay.UpdateSliders();
        }

        public void UpdateMapDisplay()
        {
            hiHatPedalDisplay.UpdateHiHatLevels();

            configWrapper.Child = new OptionButtonStack("Config:", DrumMidiDeviceConfiguration.CurrentMap.Name, delegate { Layout.Current.ShowPopup(new MidiConfigSelectionDialog()); });

            midiNoteStack.Children.Clear();
            midiNoteDisplays.Clear();

            VerticalStack midiNoteColumn = null;

            for (int midiNoteNumber = 0; midiNoteNumber < 128; midiNoteNumber++)
            {
                if ((midiNoteNumber % 20) == 0)
                {
                    midiNoteColumn = null;
                }

                if (midiNoteColumn == null)
                {
                    midiNoteColumn = new VerticalStack() { ChildSpacing = 5 };

                    midiNoteStack.Children.Add(midiNoteColumn);
                }

                MidiNoteDisplay noteDisplay = new MidiNoteDisplay() { HorizontalAlignment = EHorizontalAlignment.Stretch };
                midiNoteColumn.Children.Add(noteDisplay);

                midiNoteDisplays[midiNoteNumber] = noteDisplay;

                int nn = midiNoteNumber;

                float width;
                float height;

                Layout.Current.DefaultFont.MeasureString("000", out width, out height);

                noteDisplay.Children.Add(new TextBlock(String.Format("{0}", midiNoteNumber.ToString("D3")))
                {
                    //PressAction = delegate (UIElement element)
                    //{
                    //    NotePressed(nn);
                    //},
                    HorizontalAlignment = EHorizontalAlignment.Right,
                    DesiredWidth = width + 5
                });

                DrumVoice voice = DrumMidiDeviceConfiguration.CurrentMap.GetVoiceFromMidiNote(midiNoteNumber);

                string displayString;

                if (voice.KitPiece != EDrumKitPiece.None)
                {
                    displayString = String.Format("{0} {1}", voice.KitPiece.ToString(), DrumVoice.GetShortName(voice.Articulation));
                }
                else
                {
                    displayString = "---";
                }

                TextButton button = new TextButton(displayString)
                {
                    HorizontalAlignment = EHorizontalAlignment.Stretch,
                };

                button.ClickAction = delegate
                {
                    EditButtonClicked(nn, button);
                };

                noteDisplay.Children.Add(button);

                Layout.Current.UpdateLayout();
            }
        }

        Random random = new Random();

        void NotePressed(int midiNoteNumber)
        {
            float velocity = 0.7f + (random.Next() * 0.3f);

            //MainInterface.Instance.MixerInterface.SupressWakeActivity = true;
            //MainInterface.Instance.HandleMidiMessage(new MidiMessage(EMidiChannelCommand.NoteOn, 9, midiNoteNumber, (int)(velocity * 127)), isLive: true);
            //MainInterface.Instance.MixerInterface.SupressWakeActivity = false;
        }

        void EditButtonClicked(int midiNoteNumber, TextButton button)
        {
            Layout.Current.ShowPopup(new MidiNoteEditor(midiNoteNumber), button.ContentBounds.Center);
        }

        //public void HandleMidiMessage(AudioCore.MidiMessage message, bool isLive)
        //{
        //    DrumMidiDeviceConfiguration map = isLive ? DrumAudioHost.Instance.DrumMidiConfig : DrumMidiDeviceConfiguration.GenericMap;

        //    if (true) //isLive)
        //    {
        //        if (message.Command == EMidiChannelCommand.NoteOn)
        //        {
        //            if (message.Data2 > 0)
        //            {
        //                midiNoteNumber = message.Data1;
        //                midiNoteVelocity = message.Data2;

        //                DrumGame.Instance.AddUIWorkAction(delegate
        //                {
        //                    midiEventText.StringBuilder.Clear();
        //                    midiEventText.StringBuilder.AppendFormat("Last Note: {0} Vel: {1}", midiNoteNumber, midiNoteVelocity);
        //                });

        //                MidiNoteDisplay display = null;

        //                if (midiNoteDisplays.TryGetValue(midiNoteNumber, out display))
        //                {
        //                    display.Flash();
        //                }
        //            }
        //        }
        //        else if (message.Command == EMidiChannelCommand.Controller)
        //        {
        //            if (message.Data1 == map.HiHatPedalChannel)
        //            {
        //                hiHatPedalValue = message.Data2;

        //                hiHatPedalDisplay.SetPedalLevel((float)hiHatPedalValue / 127.0f);
        //            }
        //            else if (message.Data1 == map.SnarePositionChannel)
        //            {
        //                snarePositionValue = message.Data2;

        //                snarePositionDisplay.SetPosition((float)snarePositionValue / 127.0f);
        //            }
        //        }
        //    }
        //}

        public void LoadCustomMidiMap()
        {
            midiConfigSelector.FileAction = UpdateMidiConfig;
            midiConfigSelector.IsSaveMode = false;

            //PixGame.Instance.PushGameState("MidiConfigSelector", false);
        }

        public override void HandleInput(InputManager inputManager)
        {
            base.HandleInput(inputManager);

            if (inputManager.WasClicked("SelectMidiConfig", this))
            {
                ;
            }

            if (inputManager.WasClicked("SaveMidiConfig", this))
            {
                midiConfigSelector.FileAction = SaveMidiConfig;
                midiConfigSelector.IsSaveMode = true;

                //PixGame.Instance.PushGameState("MidiConfigSelector", false);
            }
        }
    }

    public class OptionButtonStack : HorizontalStack
    {
        public OptionButtonStack(string description, string buttonText, Action buttonAction)
        {
            HorizontalAlignment = EHorizontalAlignment.Stretch;
            ChildSpacing = 5;

            Children.Add(new TextBlock(description) { VerticalAlignment = EVerticalAlignment.Center });

            Dock buttonStack = new Dock() { HorizontalAlignment = EHorizontalAlignment.Stretch, VerticalAlignment = EVerticalAlignment.Center };
            Children.Add(buttonStack);

            buttonStack.Children.Add(new TextButton(buttonText)
            {
                VerticalAlignment = EVerticalAlignment.Center,
                HorizontalAlignment = EHorizontalAlignment.Right,
                ClickAction = buttonAction
            });
        }
    }

    public class MidiNoteEditor : InputDialog
    {
        int midiNoteNumber;
        DrumVoice voice;
        VerticalStack editStack;

        public MidiNoteEditor(int midiNoteNumber)
            : base(Layout.Current.DefaultOutlineNinePatch)
        {
            DesiredWidth = 230;

            this.midiNoteNumber = midiNoteNumber;
            voice = DrumMidiDeviceConfiguration.CurrentMap.GetVoiceFromMidiNote(midiNoteNumber);

            editStack = new VerticalStack() { HorizontalAlignment = EHorizontalAlignment.Stretch, ChildSpacing = 5 };

            SetContents(editStack);

            UpdateDisplay();

            AddInput(new DialogInput { Text = "Update", Action = UpdateVoice, CloseOnInput = true });
            AddInput(new DialogInput { Text = "Cancel", CloseOnInput = true });
        }

        void UpdateDisplay()
        {
            editStack.Children.Clear();

            editStack.Children.Add(new OptionButtonStack("Kit Piece:", voice.KitPiece.ToString(), ShowKitPieceMenu));
            editStack.Children.Add(new OptionButtonStack("Articulation:", voice.Articulation.ToString(), ShowArticulationMenu));

            Layout.Current.UpdateLayout();
        }

        void UpdateVoice()
        {
            DrumMidiDeviceConfiguration.CurrentMap.SetVoice(midiNoteNumber, voice);

            MidiEditor.Instance.UpdateMapDisplay();
        }

        void ShowKitPieceMenu()
        {
            List<MenuItem> kitPieceItems = new List<MenuItem>();

            int selected = 0;
            int i = 0;

            foreach (EDrumKitPiece kitPiece in Enum.GetValues(typeof(EDrumKitPiece)))
            {
                EDrumKitPiece k = kitPiece;

                kitPieceItems.Add(new ContextMenuItem
                {
                    Text = kitPiece.ToString(),
                    SelectAction = delegate { SetKitPiece(k); }
                });

                if (kitPiece == voice.KitPiece)
                    selected = i;

                i++;
            }

            Menu menu = new Menu(kitPieceItems);

            //menu.SelectIndex(selected, userInitiated: false);

            Layout.Current.ShowPopup(menu, ContentBounds.Center);
        }

        void SetKitPiece(EDrumKitPiece kitpiece)
        {
            voice.KitPiece = kitpiece;
            voice.Articulation = DrumVoice.GetDefaultArticulation(voice.KitPiece);

            UpdateDisplay();
        }

        void ShowArticulationMenu()
        {
            List<MenuItem> articulationItems = new List<MenuItem>();

            int selected = 0;
            int i = 0;

            foreach (EDrumArticulation articulation in DrumVoice.GetValidArticulations(DrumVoice.GetKitPieceType(voice.KitPiece)))
            {
                EDrumArticulation a = articulation;

                articulationItems.Add(new ContextMenuItem
                {
                    Text = articulation.ToString(),
                    SelectAction = delegate { SetArticulation(a); }
                });

                if (articulation == voice.Articulation)
                    selected = i;

                i++;
            }

            Menu menu = new Menu(articulationItems);

            //menu.SelectIndex(selected, userInitiated: false);

            Layout.Current.ShowPopup(menu, ContentBounds.Center);
        }

        void SetArticulation(EDrumArticulation articulation)
        {
            voice.Articulation = articulation;

            UpdateDisplay();
        }
    }

    public class HiHatLevelLine : UIElementWrapper
    {
        public bool HideIfZero { get; set; }

        UIElement levelLine;

        public override void UpdateContentLayout()
        {
            base.UpdateContentLayout();
        }

        public void SetLevel(float level)
        {
            if (HideIfZero)
            {
                if (level == 0)
                {
                    Visible = false;
                }
                else
                {
                    Visible = true;
                }
            }

            levelLine.Margin = (0, (ContentBounds.Height * level) - (levelLine.DesiredHeight / 2));

            UpdateContentLayout();
        }

        public HiHatLevelLine(UIColor color)
        {
            HorizontalAlignment = EHorizontalAlignment.Stretch;
            VerticalAlignment = EVerticalAlignment.Stretch;

            levelLine = new UIElement
            {
                BackgroundColor = color,
                HorizontalAlignment = EHorizontalAlignment.Stretch,
                VerticalAlignment = EVerticalAlignment.Absolute,
                DesiredHeight = 4,
            };

            Child = levelLine;
        }

        protected override void DrawContents()
        {
            base.DrawContents();
        }
    }

    public class HiHatPedalDisplay : NinePatchWrapper
    {
        HiHatLevelLine activePedalLine;
        HiHatLevelLine closedLine;
        HiHatLevelLine semiOpenLine;
        HiHatLevelLine openLine;
        TextBlock pedalText;
        VerticalMultiSlider slider;

        public HiHatPedalDisplay()
            : base(Layout.Current.DefaultOutlineNinePatch)
        {
            VerticalStack stack = new VerticalStack() { HorizontalAlignment = EHorizontalAlignment.Stretch, ChildSpacing = 5 };
            Child = stack;

            pedalText = new TextBlock();
            stack.Children.Add(pedalText);

            Dock pedalDock = new Dock()
            {
                DesiredHeight = 200
            };
            stack.Children.Add(pedalDock);

            closedLine = new HiHatLevelLine(UIColor.Yellow);
            pedalDock.Children.Add(closedLine);

            semiOpenLine = new HiHatLevelLine(UIColor.Orange);
            pedalDock.Children.Add(semiOpenLine);

            openLine = new HiHatLevelLine(UIColor.Red);
            pedalDock.Children.Add(openLine);

            activePedalLine = new HiHatLevelLine(UIColor.Green) { HideIfZero = true };
            pedalDock.Children.Add(activePedalLine);

            slider = new VerticalMultiSlider("VerticalPointerLeft", 3)
            {
                InvertLevel = false,
                ChangeAction = SliderChanged,
                HorizontalAlignment = EHorizontalAlignment.Right
            };
            pedalDock.Children.Add(slider);

            SetPedalLevel(0);
        }

        void SliderChanged(int slider, float value)
        {
            switch (slider)
            {
                case 0:
                    DrumMidiDeviceConfiguration.CurrentMap.HiHatPedalOpen = value;
                    break;
                case 1:
                    DrumMidiDeviceConfiguration.CurrentMap.HiHatPedalSemiOpen = value;
                    break;
                case 2:
                    DrumMidiDeviceConfiguration.CurrentMap.HiHatPedalClosed = value;
                    break;
            }

            UpdateHiHatLines();
        }

        public void UpdateHiHatLevels()
        {
            slider.SetLevel(0, DrumMidiDeviceConfiguration.CurrentMap.HiHatPedalSemiOpen);
            slider.SetLevel(2, DrumMidiDeviceConfiguration.CurrentMap.HiHatPedalClosed);

            UpdateHiHatLines();
        }

        void UpdateHiHatLines()
        {
            openLine.SetLevel(DrumMidiDeviceConfiguration.CurrentMap.HiHatPedalOpen);
            semiOpenLine.SetLevel(DrumMidiDeviceConfiguration.CurrentMap.HiHatPedalSemiOpen);
            closedLine.SetLevel(DrumMidiDeviceConfiguration.CurrentMap.HiHatPedalClosed);

            UpdateContentLayout();
        }

        public void SetPedalLevel(float level)
        {
            activePedalLine.SetLevel(level);

            pedalText.Text = String.Format("Hi Hat Pedal: {0}%", (int)(level * 100));

            UpdateContentLayout();
        }
    }

    public class MultiSlider : Dock
    {
        public Action<int, float> ChangeAction { get; set; }
        public bool InvertLevel { get; set; }
        public bool EnforceOrder { get; set; }

        protected int numSliders;
        protected float[] levels;
        protected UIImage levelImage;
        protected bool haveCapture;
        protected int activeSlider;
        protected int captureTouchID;
        protected float startLevel;
        protected float startOffset;

        bool isHorizontal;
        ImageElement[] levelImageElements;
        bool slid;

        public MultiSlider(string imageName, int numSliders, bool isHorizontal)
        {
            this.isHorizontal = isHorizontal;
            this.numSliders = numSliders;

            EnforceOrder = true;

            levelImage = Layout.Current.GetImage(imageName);

            levels = new float[numSliders];

            levelImageElements = new ImageElement[numSliders];

            for (int i = 0; i < numSliders; i++)
            {
                levelImageElements[i] = new ImageElement(levelImage)
                {
                    HorizontalAlignment = isHorizontal ? EHorizontalAlignment.Left : EHorizontalAlignment.Left,
                    VerticalAlignment = isHorizontal ? EVerticalAlignment.Top : EVerticalAlignment.Top,
                };

                Children.Add(levelImageElements[i]);
            }
        }

        public override void UpdateContentLayout()
        {
            for (int i = 0; i < numSliders; i++)
            {
                if (isHorizontal)
                {
                    levelImageElements[i].Margin = ((ContentBounds.Width * levels[i]) - (float)Math.Ceiling((levelImage.Width) / 2f), 0);
                }
                else
                {
                    levelImageElements[i].Margin = (0, (ContentBounds.Height * levels[i]) - (float)Math.Ceiling((levelImage.Height) / 2f));
                }
            }

            base.UpdateContentLayout();
        }

        public void SetLevel(int sliderIndex, float level)
        {
            UpdateLevel(sliderIndex, InvertLevel ? (1.0f - level) : level, enforceOrder: false, sendUpdate: false);
        }

        void UpdateLevel(int sliderIndex, float level, bool enforceOrder, bool sendUpdate)
        {
            level = Math.Max(Math.Min(level, 1.0f), 0);

            int newActiveSlider = activeSlider;

            if (enforceOrder)
            {
                if (sliderIndex > 0)
                {
                    if (level < levels[sliderIndex - 1])
                    {
                        level = levels[sliderIndex - 1];

                        if (!slid)
                        {
                            newActiveSlider = sliderIndex - 1;
                        }
                    }
                }

                if (sliderIndex < (numSliders - 1))
                {
                    if (level > levels[sliderIndex + 1])
                    {
                        level = levels[sliderIndex + 1];

                        if (!slid)
                        {
                            newActiveSlider = sliderIndex + 1;
                        }
                    }
                }
            }

            levels[sliderIndex] = level;

            if (sendUpdate && (ChangeAction != null))
            {
                ChangeAction(activeSlider, InvertLevel ? 1.0f - levels[activeSlider] : levels[activeSlider]);
            }

            activeSlider = newActiveSlider;

            UpdateContentLayout();
        }

        int GetClosestSlider(Vector2 position)
        {
            float offset = (isHorizontal ? (position.X - ContentBounds.X) : (position.Y - ContentBounds.Y)) /
                (isHorizontal ? ContentBounds.Width : ContentBounds.Height);

            int closestIndex = 0;
            float closestDist = float.MaxValue;

            for (int i = 0; i < numSliders; i++)
            {
                float dist = Math.Abs(levels[i] - offset);

                if ((dist < closestDist) || ((levels[i] < 1.0f) && (dist == closestDist)))
                {
                    closestIndex = i;
                    closestDist = dist;
                }
            }

            return closestIndex;
        }

        public override bool HandleTouch(in Touch touch)
        {
            if (IsTap(touch))
            {
                float offset = (isHorizontal ? (touch.Position.X - ContentBounds.X) : (touch.Position.Y - ContentBounds.Y)) /
                    (isHorizontal ? ContentBounds.Width : ContentBounds.Height);

                UpdateLevel(activeSlider, offset, EnforceOrder, sendUpdate: true);
            }

            if (touch.TouchState == ETouchState.Pressed)
            {
                haveCapture = true;
                captureTouchID = touch.TouchID;

                activeSlider = GetClosestSlider(touch.Position);

                startLevel = levels[activeSlider];
                startOffset = isHorizontal ? touch.Position.X : touch.Position.Y;
                slid = false;

                CaptureTouch(touch);

                return true;
            }
            else if (touch.TouchState == ETouchState.Moved)
            {
                float delta = ((isHorizontal ? touch.Position.X : touch.Position.Y) - startOffset) / (isHorizontal ? ContentBounds.Width : ContentBounds.Height);

                if (Math.Abs(levels[activeSlider] - startLevel) > 0.01f)
                {
                    slid = true;
                }

                UpdateLevel(activeSlider, startLevel + delta, EnforceOrder, sendUpdate: true);
            }
            else if (touch.TouchState == ETouchState.Released)
            {
                if (touch.TouchID == captureTouchID)
                {
                    ReleaseTouch(captureTouchID);

                    haveCapture = false;

                    return true;
                }
            }

            return base.HandleTouch(touch);
        }
    }

    public class VerticalMultiSlider : MultiSlider
    {
        public VerticalMultiSlider(int numSliders)
            : this("VerticalPointer", numSliders)
        {
        }

        public VerticalMultiSlider(string imageName, int numSliders)
            : base(imageName, numSliders, isHorizontal: false)
        {
        }

        protected override void GetContentSize(out float width, out float height)
        {
            base.GetContentSize(out width, out height);

            height = 0;
        }

        //public override void SetLayout(UILayout layout, UIElement parent)
        //{
        //    base.SetLayout(layout, parent);

        //    InputLayout = new UILayout(new Vector2(layout.Offset.X - (levelImage.Width * PixUI.DefaultScale), layout.Offset.Y), layout.Width + (levelImage.Width * PixUI.DefaultScale), layout.Height);
        //}
    }

    public class HorizontalMultiSlider : MultiSlider
    {
        public HorizontalMultiSlider(int numSliders)
            : this("HorizontalSlider", numSliders)
        {
        }

        public HorizontalMultiSlider(string imageName, int numSliders)
            : base(imageName, numSliders, isHorizontal: true)
        {
            Children.Insert(0, new UIElement
            {
                HorizontalAlignment = EHorizontalAlignment.Stretch,
                VerticalAlignment = EVerticalAlignment.Center,
                BackgroundColor = UIColor.Black,
                DesiredHeight = 4
            });
        }

        protected override void GetContentSize(out float width, out float height)
        {
            base.GetContentSize(out width, out height);

            width = 0;
        }
    }
}
