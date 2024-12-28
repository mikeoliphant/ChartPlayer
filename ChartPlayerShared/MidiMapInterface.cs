using System;
using System.Collections.Generic;
using System.IO;
using UILayout;

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
        public static string ConfigPath { get; set; }
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

            VerticalStack mainVStack = new VerticalStack { HorizontalAlignment = EHorizontalAlignment.Stretch, VerticalAlignment = EVerticalAlignment.Stretch };
            Children.Add(mainVStack);

            string midiUserPath = Path.Combine(ConfigPath, "MidiMaps");

            if (!Directory.Exists(midiUserPath))
                Directory.CreateDirectory(midiUserPath);

            //midiConfigSelector = new FileSelector("Midi Configuration", canCreateFolders: true);
            //midiConfigSelector.AllowedExtensions = new string[] { ".xml" };
            //midiConfigSelector.SetRootPath(midiUserPath);
            //midiConfigSelector.FileAction = UpdateMidiConfig;

            //PixGame.Instance.AddGameState("MidiConfigSelector", midiConfigSelector);

            HorizontalStack mainStack = new HorizontalStack { HorizontalAlignment = EHorizontalAlignment.Stretch, VerticalAlignment = EVerticalAlignment.Stretch };
            mainVStack.Children.Add(mainStack);

            VerticalStack leftStack = new VerticalStack
            {
                DesiredWidth = 200,
                ChildSpacing = 5
            };
            mainStack.Children.Add(leftStack);

            NinePatchWrapper configPanel = new NinePatchWrapper
            {
                HorizontalAlignment = EHorizontalAlignment.Stretch
            };
            leftStack.Children.Add(configPanel);

            VerticalStack configStack = new VerticalStack { ChildSpacing = 5, HorizontalAlignment = EHorizontalAlignment.Stretch };

            configPanel.Child = configStack;

            configWrapper = new UIElementWrapper() { HorizontalAlignment = EHorizontalAlignment.Stretch };
            configStack.Children.Add(configWrapper);

            configStack.Children.Add(new TextButton("Save Midi Config") { HorizontalAlignment = EHorizontalAlignment.Stretch });

            midiNoteStack = new HorizontalStack() { HorizontalAlignment = EHorizontalAlignment.Center, VerticalAlignment = EVerticalAlignment.Top, ChildSpacing = 20 };
            mainStack.Children.Add(midiNoteStack);

            NinePatchWrapper noteDisplay = new NinePatchWrapper() { HorizontalAlignment = EHorizontalAlignment.Stretch };
            leftStack.Children.Add(noteDisplay);
            midiEventText = new TextBlock("Last Note: None");
            noteDisplay.Child = midiEventText;

            hiHatPedalDisplay = new HiHatPedalDisplay() { HorizontalAlignment = EHorizontalAlignment.Stretch };
            leftStack.Children.Add(hiHatPedalDisplay);

            //snarePositionDisplay = new PositionalSensingDisplay() { HorizontalAlignment = EHorizontalAlignment.Stretch };
            //leftStack.Children.Add(snarePositionDisplay);
        }

        public void UpdateMidiConfig(string path)
        {
            //MixerSettings.Instance.MidiConfig = path;

            SetMidiConfig(path);

            //MixerSettings.SaveXml(Path.Combine(DrumAudioHost.UserDataFolder, "MixerSettings.xml"));
        }

        void SaveMidiConfig(string path)
        {
            MixerSettings.Instance.MidiConfig = path;

            configWrapper.Child = new OptionButtonStack("Config:", Path.GetFileNameWithoutExtension(MixerSettings.Instance.MidiConfig), "SelectMidiConfig");

            DrumAudioHost.Instance.DrumMidiConfig.SaveXml(MixerSettings.Instance.MidiConfig);

            MixerSettings.SaveXml(Path.Combine(DrumAudioHost.UserDataFolder, "MixerSettings.xml"));

            PixGame.Instance.UserInterface.NeedLayoutUpdate = true;
        }

        public void SetMidiConfig(string path)
        {
            configWrapper.Child = new OptionButtonStack("Config:", Path.GetFileNameWithoutExtension(MixerSettings.Instance.MidiConfig), "SelectMidiConfig");

            DrumAudioHost.Instance.DrumMidiConfig = DrumMidiDeviceConfiguration.LoadFromXml(path);

            if (DrumAudioHost.Instance.DrumMidiConfig == null)
            {
                DrumAudioHost.Instance.DrumMidiConfig = DrumMidiDeviceConfiguration.GenericMap;
            }

            UpdateMapDisplay();
            hiHatPedalDisplay.UpdateHiHatLevels();
            snarePositionDisplay.UpdateSliders();
        }

        public void UpdateMapDisplay()
        {
            midiNoteStack.Children.Clear();
            midiNoteDisplays.Clear();

            VerticalStack midiNoteColumn = null;

            for (int midiNoteNumber = 0; midiNoteNumber < 128; midiNoteNumber++)
            {
                if ((midiNoteNumber % 26) == 0)
                {
                    midiNoteColumn = null;
                }

                if (midiNoteColumn == null)
                {
                    midiNoteColumn = new VerticalStack() { ChildSpacing = PixUI.DefaultScale };

                    midiNoteStack.Children.Add(midiNoteColumn);
                }

                MidiNoteDisplay noteDisplay = new MidiNoteDisplay() { HorizontalAlignment = EHorizontalAlignment.Stretch };
                midiNoteColumn.Children.Add(noteDisplay);

                midiNoteDisplays[midiNoteNumber] = noteDisplay;

                int nn = midiNoteNumber;

                noteDisplay.Children.Add(new TextBlock(String.Format("{0}", midiNoteNumber.ToString("D3")))
                {
                    PressAction = delegate (UIElement element)
                    {
                        NotePressed(nn);
                    },
                    HorizontalAlignment = EHorizontalAlignment.Right,
                    Width = PixUI.DefaultScale * 35
                });

                DrumVoice voice = DrumAudioHost.Instance.DrumMidiConfig.GetVoiceFromMidiNote(midiNoteNumber);

                string displayString;

                if (voice.KitPiece != EDrumKitPiece.None)
                {
                    displayString = String.Format("{0} {1}", voice.KitPiece.ToString(), DrumVoice.GetShortName(voice.Articulation));
                }
                else
                {
                    displayString = "---";
                }

                noteDisplay.Children.Add(new TextTouchButton(displayString)
                {
                    TextVerticalPadding = 0,
                    HorizontalAlignment = EHorizontalAlignment.Stretch,
                    ClickAction = delegate (UIElement element) { EditButtonClicked(nn); }
                });

                PixGame.Instance.UserInterface.NeedLayoutUpdate = true;
            }
        }

        void NotePressed(int midiNoteNumber)
        {
            float velocity = 0.7f + (PixGame.Random.Next() * 0.3f);

            MainInterface.Instance.MixerInterface.SupressWakeActivity = true;
            MainInterface.Instance.HandleMidiMessage(new MidiMessage(EMidiChannelCommand.NoteOn, 9, midiNoteNumber, (int)(velocity * 127)), isLive: true);
            MainInterface.Instance.MixerInterface.SupressWakeActivity = false;
        }

        void EditButtonClicked(int midiNoteNumber)
        {
            MainInterface.Instance.ShowPopup(new MidiNoteEditor(midiNoteNumber));
        }

        public void HandleMidiMessage(AudioCore.MidiMessage message, bool isLive)
        {
            DrumMidiDeviceConfiguration map = isLive ? DrumAudioHost.Instance.DrumMidiConfig : DrumMidiDeviceConfiguration.GenericMap;

            if (true) //isLive)
            {
                if (message.Command == EMidiChannelCommand.NoteOn)
                {
                    if (message.Data2 > 0)
                    {
                        midiNoteNumber = message.Data1;
                        midiNoteVelocity = message.Data2;

                        DrumGame.Instance.AddUIWorkAction(delegate
                        {
                            midiEventText.StringBuilder.Clear();
                            midiEventText.StringBuilder.AppendFormat("Last Note: {0} Vel: {1}", midiNoteNumber, midiNoteVelocity);
                        });

                        MidiNoteDisplay display = null;

                        if (midiNoteDisplays.TryGetValue(midiNoteNumber, out display))
                        {
                            display.Flash();
                        }
                    }
                }
                else if (message.Command == EMidiChannelCommand.Controller)
                {
                    if (message.Data1 == map.HiHatPedalChannel)
                    {
                        hiHatPedalValue = message.Data2;

                        hiHatPedalDisplay.SetPedalLevel((float)hiHatPedalValue / 127.0f);
                    }
                    else if (message.Data1 == map.SnarePositionChannel)
                    {
                        snarePositionValue = message.Data2;

                        snarePositionDisplay.SetPosition((float)snarePositionValue / 127.0f);
                    }
                }
            }
        }

        public void LoadCustomMidiMap()
        {
            midiConfigSelector.FileAction = UpdateMidiConfig;
            midiConfigSelector.IsSaveMode = false;

            PixGame.Instance.PushGameState("MidiConfigSelector", false);
        }

        public override void HandleInput(PixInputManager inputManager)
        {
            base.HandleInput(inputManager);

            if (inputManager.WasClicked("SelectMidiConfig"))
            {
                (PixGame.Instance.CurrentGameState as PopupGameState).ShowPopup(new MidiConfigSelectionDialog());
            }

            if (inputManager.WasClicked("SaveMidiConfig"))
            {
                midiConfigSelector.FileAction = SaveMidiConfig;
                midiConfigSelector.IsSaveMode = true;

                PixGame.Instance.PushGameState("MidiConfigSelector", false);
            }
        }
    }

    public class MidiNoteEditor : DrumDialog
    {
        int midiNoteNumber;
        DrumVoice voice;
        VerticalStack editStack;

        public MidiNoteEditor(int midiNoteNumber)
        {
            Width = PixUI.DefaultScale * 230;

            this.midiNoteNumber = midiNoteNumber;
            voice = DrumAudioHost.Instance.DrumMidiConfig.GetVoiceFromMidiNote(midiNoteNumber);

            editStack = new VerticalStack() { HorizontalAlignment = EHorizontalAlignment.Stretch, ChildSpacing = DrumUI.DefaultPadding };

            SetContents(editStack);

            UpdateDisplay();

            AddInput(new DialogInput { Text = "Update", ButtonName = "Continue", Action = UpdateVoice, CloseOnInput = true });
            AddInput(new DialogInput { Text = "Cancel", ButtonName = "MenuBack", CloseOnInput = true });
        }

        public override void HandleInput(PixInputManager inputManager)
        {
            base.HandleInput(inputManager);

            if (inputManager.WasClicked("EditKitPiece"))
            {
                ShowKitPieceMenu();
            }
            else if (inputManager.WasClicked("EditArticulation"))
            {
                ShowArticulationMenu();
            }
        }

        void UpdateDisplay()
        {
            editStack.Children.Clear();

            editStack.Children.Add(new OptionButtonStack("Kit Piece:", voice.KitPiece.ToString(), "EditKitPiece"));
            editStack.Children.Add(new OptionButtonStack("Articulation:", voice.Articulation.ToString(), "EditArticulation"));

            PixGame.Instance.UserInterface.NeedLayoutUpdate = true;
        }

        void UpdateVoice()
        {
            DrumAudioHost.Instance.DrumMidiConfig.SetVoice(midiNoteNumber, voice);

            MainInterface.Instance.MidiEditor.UpdateMapDisplay();
        }

        void ShowKitPieceMenu()
        {
            List<ContextMenuItem> kitPieceItems = new List<ContextMenuItem>();

            int selected = 0;
            int i = 0;

            foreach (EDrumKitPiece kitPiece in Enum.GetValues(typeof(EDrumKitPiece)))
            {
                EDrumKitPiece k = kitPiece;

                kitPieceItems.Add(new ContextMenuItem
                {
                    Name = kitPiece.ToString(),
                    SelectAction = delegate { SetKitPiece(k); }
                });

                if (kitPiece == voice.KitPiece)
                    selected = i;

                i++;
            }

            DrumContextMenu menu = new DrumContextMenu(kitPieceItems);

            menu.SelectIndex(selected, userInitiated: false);

            MainInterface.Instance.ShowContextUIWithOutline(menu, ContentLayout.Center);
        }

        void SetKitPiece(EDrumKitPiece kitpiece)
        {
            voice.KitPiece = kitpiece;
            voice.Articulation = DrumVoice.GetDefaultArticulation(voice.KitPiece);

            UpdateDisplay();
        }

        void ShowArticulationMenu()
        {
            List<ContextMenuItem> articulationItems = new List<ContextMenuItem>();

            int selected = 0;
            int i = 0;

            foreach (EDrumArticulation articulation in DrumVoice.GetValidArticulations(DrumVoice.GetKitPieceType(voice.KitPiece)))
            {
                EDrumArticulation a = articulation;

                articulationItems.Add(new ContextMenuItem
                {
                    Name = articulation.ToString(),
                    SelectAction = delegate { SetArticulation(a); }
                });

                if (articulation == voice.Articulation)
                    selected = i;

                i++;
            }

            DrumContextMenu menu = new DrumContextMenu(articulationItems);

            menu.SelectIndex(selected, userInitiated: false);

            MainInterface.Instance.ShowContextUIWithOutline(menu, ContentLayout.Center);
        }

        void SetArticulation(EDrumArticulation articulation)
        {
            voice.Articulation = articulation;

            UpdateDisplay();
        }
    }

    public class HiHatLevelLine : AbsoluteElementWrapper
    {
        public bool HideIfZero { get; set; }

        UIElement levelLine;

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

            YOffset = (ContentLayout.Height * level) - (levelLine.Height / 2);

            UpdateContentLayout();
        }

        public HiHatLevelLine(UIColor color)
        {
            levelLine = new UIElement
            {
                BackgroundColor = color,
                HorizontalAlignment = EHorizontalAlignment.Stretch,
                Height = PixUI.DefaultScale * 4,
            };

            Child = levelLine;
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
        {
            VerticalStack stack = new VerticalStack() { HorizontalAlignment = EHorizontalAlignment.Stretch, ChildSpacing = DrumUI.DefaultPadding };
            Child = stack;

            pedalText = new TextBlock();
            stack.Children.Add(pedalText);

            Dock pedalDock = new Dock()
            {
                BackgroundColor = UIColor.Black,
                Height = PixUI.DefaultScale * 200
            };
            stack.Children.Add(pedalDock);

            closedLine = new HiHatLevelLine(DrumUI.PanelBackgroundColorDark);
            pedalDock.Children.Add(closedLine);

            semiOpenLine = new HiHatLevelLine(DrumUI.PanelBackgroundColorLight);
            pedalDock.Children.Add(semiOpenLine);

            openLine = new HiHatLevelLine(DrumUI.PanelForegroundColor);
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
                    DrumAudioHost.Instance.DrumMidiConfig.HiHatPedalOpen = value;
                    break;
                case 1:
                    DrumAudioHost.Instance.DrumMidiConfig.HiHatPedalSemiOpen = value;
                    break;
                case 2:
                    DrumAudioHost.Instance.DrumMidiConfig.HiHatPedalClosed = value;
                    break;
            }

            UpdateHiHatLines();
        }

        public void UpdateHiHatLevels()
        {
            slider.SetLevel(0, DrumAudioHost.Instance.DrumMidiConfig.HiHatPedalOpen);
            slider.SetLevel(1, DrumAudioHost.Instance.DrumMidiConfig.HiHatPedalSemiOpen);
            slider.SetLevel(2, DrumAudioHost.Instance.DrumMidiConfig.HiHatPedalClosed);

            UpdateHiHatLines();
        }

        void UpdateHiHatLines()
        {
            openLine.SetLevel(DrumAudioHost.Instance.DrumMidiConfig.HiHatPedalOpen);
            semiOpenLine.SetLevel(DrumAudioHost.Instance.DrumMidiConfig.HiHatPedalSemiOpen);
            closedLine.SetLevel(DrumAudioHost.Instance.DrumMidiConfig.HiHatPedalClosed);

            UpdateContentLayout();
        }

        public void SetPedalLevel(float level)
        {
            activePedalLine.SetLevel(level);

            pedalText.Text = String.Format("Hi Hat Pedal: {0}%", (int)(level * 100));

            UpdateContentLayout();
        }
    }
}
