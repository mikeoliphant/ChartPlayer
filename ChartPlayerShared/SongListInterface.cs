using System;
using System.Collections.Generic;
using System.Linq;
using UILayout;
using SongFormat;
using System.Reflection.Metadata.Ecma335;

namespace ChartPlayer
{
    public class SongListDisplay : HorizontalStack, IPopup
    {
        public Action CloseAction { get; set; }
        public ESongInstrumentType CurrentInstrument { get; private set; } = ESongInstrumentType.LeadGuitar;

        public MultiColumnItemDisplay<SongIndexEntry> SongList { get; private set; }

        SongIndexEntry selectedSong;

        SongIndex songIndex;
        List<SongIndexEntry> allSongs;
        List<SongIndexEntry> currentSongs;
        ItemDisplayColum<SongIndexEntry> tuningColumn;
        HorizontalStack songDisplayStack;
        ImageElement albumImage;
        TextBlock songTitleText;
        TextBlock songArtistText;
        TextBlock songAlbumText;
        HorizontalStack arrangementStack;
        DialogInputStack buttonInputStack;
        NinePatchWrapper bottomInterface;

        public SongListDisplay()
        {
            HorizontalAlignment = EHorizontalAlignment.Stretch;
            VerticalAlignment = EVerticalAlignment.Stretch;

            Padding = new LayoutPadding(5);

            SongList = new MultiColumnItemDisplay<SongIndexEntry>(UIColor.Black);

            Children.Add(SongList);

            SongList.ListDisplay.SelectAction = SongSelected;

            ItemDisplayColum<SongIndexEntry> titleColumn = new ItemDisplayColum<SongIndexEntry> { DisplayName = "Title", PropertyName = "SongName" };
            ItemDisplayColum<SongIndexEntry> artistColumn = new ItemDisplayColum<SongIndexEntry> { DisplayName = "Artist", PropertyName = "ArtistName", SecondarySortColumn = titleColumn };

            SongList.AddColumn(titleColumn);
            SongList.AddColumn(artistColumn);
            SongList.AddColumn(new ItemDisplayColum<SongIndexEntry>
            {
                DisplayName = "Parts",
                PropertyName = "Arrangements",
                RequestedDisplayWidth = 50,
                SecondarySortColumn = artistColumn
            });
            SongList.AddColumn(new ItemDisplayColum<SongIndexEntry>
            {
                DisplayName = "Plays",
                ValueFunc = delegate (SongIndexEntry entry) { return (entry.Stats[(int)CurrentInstrument] == null) ? 0 : entry.Stats[(int)CurrentInstrument].NumPlays; },
                RequestedDisplayWidth = 50,
                StartReversed = true,
                SecondarySortColumn = artistColumn
            });
            SongList.AddColumn(new ItemDisplayColum<SongIndexEntry>
            {
                DisplayName = "Last Play",
                ValueFunc = delegate (SongIndexEntry entry) { return (entry.Stats[(int)CurrentInstrument] == null) ? "" : GetDayString(entry.Stats[(int)CurrentInstrument].LastPlayed); },
                SortValueFunc = delegate (SongIndexEntry entry) { return (entry.Stats[(int)CurrentInstrument] == null) ? DateTime.MinValue : entry.Stats[(int)CurrentInstrument].LastPlayed; },
                RequestedDisplayWidth = 80,
                StartReversed = true,
                SecondarySortColumn = artistColumn
            });

            float width = 0;
            float height = 0;

            SongList.ListDisplay.Font.SpriteFont.MeasureString("EbEbEbEbEbEb C8", out width, out height);
            SongList.AddColumn(tuningColumn = new ItemDisplayColum<SongIndexEntry>
            {
                DisplayName = "Tuning",
                PropertyName = "LeadGuitarTuning",
                RequestedDisplayWidth = width,
                SecondarySortColumn = artistColumn
            });

            bottomInterface = new NinePatchWrapper(Layout.Current.GetImage("PopupBackground"))
            {
                HorizontalAlignment = EHorizontalAlignment.Stretch
            };
            SongList.BottomDock.Children.Add(bottomInterface);

            HorizontalStack bottomStack = new HorizontalStack();
            bottomInterface.Child = bottomStack;

            songDisplayStack = new HorizontalStack()
            {
                VerticalAlignment = EVerticalAlignment.Stretch,
                ChildSpacing = 10,
                Visible = false
            };
            bottomStack.Children.Add(songDisplayStack);

            albumImage = new ImageElement("SingleWhitePixel")
            {
                DesiredWidth = 192,
                DesiredHeight = 192
            };
            songDisplayStack.Children.Add(albumImage);

            VerticalStack songInfoStack = new VerticalStack()
            {
                DesiredWidth = 400,
                VerticalAlignment = EVerticalAlignment.Stretch
            };
            songDisplayStack.Children.Add(songInfoStack);

            songInfoStack.Children.Add(songTitleText = new TextBlock());
            songInfoStack.Children.Add(songArtistText = new TextBlock());
            songInfoStack.Children.Add(songAlbumText = new TextBlock());

            songInfoStack.Children.Add(new UIElement { VerticalAlignment = EVerticalAlignment.Stretch });

            arrangementStack = new HorizontalStack { ChildSpacing = 2 };
            songInfoStack.Children.Add(arrangementStack);

            HorizontalStack buttonStack = new HorizontalStack
            {
                HorizontalAlignment = EHorizontalAlignment.Stretch,
                VerticalAlignment = EVerticalAlignment.Bottom,
                Padding = new LayoutPadding(0, 5),
                ChildSpacing = 2
            };

            buttonInputStack = new DialogInputStack(this, buttonStack);
            bottomStack.Children.Add(buttonInputStack);

            buttonInputStack.AddInput(new DialogInput { Text = "Lead", Action = delegate { SetCurrentInstrument(ESongInstrumentType.LeadGuitar); } });
            buttonInputStack.AddInput(new DialogInput { Text = "Rhythm", Action = delegate { SetCurrentInstrument(ESongInstrumentType.RhythmGuitar); } });
            buttonInputStack.AddInput(new DialogInput { Text = "Bass", Action = delegate { SetCurrentInstrument(ESongInstrumentType.BassGuitar); } });
            //buttonInputStack.AddInput(new DialogInput { Text = "Keys", Action = delegate { SetCurrentInstrument(ESongInstrumentType.Keys); } });
            buttonInputStack.AddInput(new DialogInput { Text = "ReScan", Action = delegate { SongPlayerInterface.Instance.RescanSongIndex(); } });
            buttonInputStack.AddInput(new DialogInput
            {
                Text = "?",
                Action = delegate
                {
                    Layout.Current.ShowPopup(new HelpDialog(new TextBlock("Click to select song.\n\nDrag to scroll, or use arrow keys and page up/down.\n\nClick column titles to sort.")));
                }
            });
            buttonInputStack.AddInput(new DialogInput { Text = "Close", Action = Close });

            SongList.SetSortColumn("Artist");

            CloseAction = Close;
        }

        public virtual void Opened()
        {
        }

        public void SetSongIndex(SongIndex songIndex)
        {
            this.songIndex = songIndex;

            this.allSongs = songIndex.Songs;

            SetCurrentSongs();
        }

        void SetCurrentSongs()
        {
            switch (CurrentInstrument)
            {
                case ESongInstrumentType.LeadGuitar:
                    currentSongs = allSongs.Where(s => (s.LeadGuitarTuning != null)).ToList();
                    break;
                case ESongInstrumentType.RhythmGuitar:
                    currentSongs = allSongs.Where(s => (s.RhythmGuitarTuning != null)).ToList();
                    break;
                case ESongInstrumentType.BassGuitar:
                    currentSongs = allSongs.Where(s => (s.BassGuitarTuning != null)).ToList();
                    break;
                case ESongInstrumentType.Keys:
                    currentSongs = allSongs;
                    break;
                case ESongInstrumentType.Vocals:
                    currentSongs = allSongs;
                    break;
                default:
                    currentSongs = allSongs;
                    break;
            }

            SongList.SetItems(currentSongs);

            SongList.Sort();
        }

        public void SetCurrentInstrument(ESongInstrumentType type)
        {
            if (CurrentInstrument != type)
            {
                CurrentInstrument = type;

                tuningColumn.PropertyName = type.ToString() + "Tuning";

                if ((currentSongs == null) || (currentSongs.Count == 0))
                    return;

                SongIndexEntry topSong = currentSongs[SongList.ListDisplay.CurrentTopItemIndex];

                SetCurrentSongs();

                int songIndex = -1;

                if (selectedSong != null)
                {
                    songIndex = SongList.ListDisplay.Items.IndexOf(selectedSong);
                }

                if (songIndex != -1)
                {
                    SongList.ListDisplay.SetTopItem(songIndex);
                    SongList.ListDisplay.LastSelectedItem = songIndex;
                }
                else
                {
                    int topIndex = SongList.GetIndexOf(topSong);

                    SongList.ListDisplay.SetTopItem(topIndex);

                    SongList.ListDisplay.LastSelectedItem = -1;
                }

                if (selectedSong != null)
                    UpdateSelectedSongDisplay();
            }
        }

        public void Play()
        {
            if (selectedSong != null)
            {
                Close();

                SongPlayerInterface.Instance.SetSong(selectedSong, CurrentInstrument, null);
            }
        }

        public void SongSelected(int index)
        {
            if (currentSongs[index] == selectedSong)
            {
                Play();
            }
            else
            {
                selectedSong = currentSongs[index];

                songDisplayStack.Visible = true;

                UpdateSelectedSongDisplay();
            }
        }

        UIImage albumArtToDelete = null;

        void UpdateSelectedSongDisplay()
        {
            if (albumArtToDelete != null)
            {
                if (albumArtToDelete.Texture != null)
                {
                    albumArtToDelete.Texture.Dispose();
                }

                albumArtToDelete = null;
            }

            if (albumImage.Image.Width > 1) // Don't dispose our single white pixel image
            {
                albumArtToDelete = albumImage.Image;
            }

            albumImage.Image = songIndex.GetAlbumImage(selectedSong);

            songTitleText.Text = selectedSong.SongName;
            songArtistText.Text = selectedSong.ArtistName;
            songAlbumText.Text = selectedSong.AlbumName;

            arrangementStack.Children.Clear();

            SongData songData = songIndex.GetSongData(selectedSong);

            var parts = songData.InstrumentParts.Where(p => p.InstrumentType == CurrentInstrument);

            int count = parts.Count();

            if (count > 0)
            {
                foreach (SongInstrumentPart part in parts)
                {
                    arrangementStack.Children.Add(new TextButton((count == 1) ? "Play" : "Play \"" + part.InstrumentName + "\"")
                    {
                        ClickAction = delegate
                        {
                            Close();

                            SongPlayerInterface.Instance.SetSong(selectedSong, CurrentInstrument, part.InstrumentName);
                        }
                    });
                }
            }

            UpdateContentLayout();
        }

        public void Close()
        {
            Layout.Current.ClosePopup(this);
        }

        DateTime currentDate = DateTime.Now;

        string GetDayString(DateTime date)
        {
            int days = (currentDate.Date - date.Date).Days;

            if (days == 0)
                return "Today";

            if (days > 365)
            {
                return (days / 365 + "y");
            }
                
            if (days > 30)
            {
                return (days / 30) + "mo";
            }

            return days + "d";
        }
    }

    public class VerticalStackWidthDrawBehind : VerticalStack
    {
        public UIElement DrawBehindElement { get; set; }

        protected override void DrawContents()
        {
            if (!Visible)
                return;

            if (DrawBehindElement != null)
            {
                DrawBehindElement.Draw();
            }

            if (DrawInReverse)
            {
                for (int i = children.Count - 1; i >= 0; i--)
                {
                    if (children[i] != DrawBehindElement)
                    {
                        children[i].Draw();
                    }
                }
            }
            else
            {
                foreach (UIElement child in children)
                {
                    if (child != DrawBehindElement)
                    {
                        child.Draw();
                    }
                }
            }
        }
    }

    public class ItemDisplayColum<T>
    {
        public string PropertyName { get; set; }
        public Func<T, object> ValueFunc { get; set; }
        public string SortPropertyName { get; set; }
        public Func<T, object> SortValueFunc { get; set; }
        public bool StartReversed { get; set; }
        public ItemDisplayColum<T> SecondarySortColumn { get; set; }
        public string DisplayName { get; set; }
        public float DisplayOffset { get; set; }
        public float DisplayWidth { get; set; }
        public float RequestedDisplayWidth { get; set; }

        public object GetSortValue(T obj)
        {
            if (SortValueFunc != null)
                return SortValueFunc(obj);

            if (ValueFunc != null)
                return ValueFunc(obj);

            return obj.GetType().GetProperty(SortPropertyName ?? PropertyName).GetValue(obj, null);
        }

        public object GetValue(T obj)
        {
            if (ValueFunc != null)
                return ValueFunc(obj);

            return obj.GetType().GetProperty(PropertyName).GetValue(obj, null);
        }

        public string GetDisplayValue(T obj)
        {
            object prop = GetValue(obj);

            if (prop == null)
                return "";

            return prop.ToString();
        }

        public int Compare(T a, T b)
        {
            object aObj = GetSortValue(a);
            object bObj = GetSortValue(b);

            if (aObj == null)
            {
                if (bObj == null)
                    return 0;

                return 1;
            }

            if (bObj == null)
                return -1;

            int compare = ((IComparable)aObj).CompareTo((IComparable)bObj);

            if ((aObj is string) && ((aObj as string).StartsWith("Men")) && ((bObj as string).StartsWith("Men")))
            {

            }

            if ((compare == 0) && (SecondarySortColumn != null))
            {
                return SecondarySortColumn.Compare(a, b);
            }

            return compare;
        }

        public int CompareReverse(T a, T b)
        {
            return Compare(b, a);
        }
    }

    public class MultiColumnItemDisplaySwipeList<T> : SwipeList
    {
        MultiColumnItemDisplay<T> parentDisplay;

        public MultiColumnItemDisplaySwipeList(MultiColumnItemDisplay<T> parentDisplay)
        {
            this.parentDisplay = parentDisplay;
        }

        public override void DrawItemContents(int item, float x, float y)
        {
            foreach (ItemDisplayColum<T> column in parentDisplay.DisplayColumns)
            {
                Layout.Current.GraphicsContext.DrawText(column.GetDisplayValue((T)Items[item]), Font, (int)(x + column.DisplayOffset), (int)y, TextColor, FontScale);
            }
        }
    }

    public class MultiColumnItemDisplay<T> : Dock
    {
        public MultiColumnItemDisplaySwipeList<T> ListDisplay { get; private set; }
        internal List<ItemDisplayColum<T>> DisplayColumns { get; private set; }
        public Dock BottomDock { get; private set; }
        public ItemDisplayColum<T> CurrentSortColumn { get; private set; }
        public bool CurrentSortReverse { get; set; }

        List<T> items;
        Dock topDock;
        HorizontalStack headerStack;

        public MultiColumnItemDisplay(UIColor backgroundColor)
        {
            this.BackgroundColor = backgroundColor;

            DisplayColumns = new List<ItemDisplayColum<T>>();

            VerticalStackWidthDrawBehind swipeStack = new VerticalStackWidthDrawBehind()
            {
                HorizontalAlignment = EHorizontalAlignment.Stretch,
                VerticalAlignment = EVerticalAlignment.Stretch
            };
            Children.Add(swipeStack);

            swipeStack.Children.Add(topDock = new Dock
            {
                VerticalAlignment = EVerticalAlignment.Top,
                HorizontalAlignment = EHorizontalAlignment.Stretch,
                Padding = new LayoutPadding(0, 5),
                BackgroundColor = backgroundColor
            });

            headerStack = new HorizontalStack { HorizontalAlignment = EHorizontalAlignment.Stretch };
            topDock.Children.Add(headerStack);

            HorizontalStack scrollStack = new HorizontalStack()
            {
                HorizontalAlignment = EHorizontalAlignment.Stretch,
                VerticalAlignment = EVerticalAlignment.Stretch,
                Padding = new LayoutPadding(5)
            };
            swipeStack.Children.Add(scrollStack);
            swipeStack.DrawBehindElement = scrollStack;

            ListDisplay = new MultiColumnItemDisplaySwipeList<T>(this);
            ListDisplay.HorizontalAlignment = EHorizontalAlignment.Stretch;
            ListDisplay.VerticalAlignment = EVerticalAlignment.Stretch;
            scrollStack.Children.Add(ListDisplay);

            VerticalScrollBarWithArrows scrollBar = new VerticalScrollBarWithArrows()
            {
                VerticalAlignment = EVerticalAlignment.Stretch,
            };
            scrollStack.Children.Add(scrollBar);

            ListDisplay.SetScrollBar(scrollBar.ScrollBar);
            scrollBar.ScrollBar.Scrollable = ListDisplay;

            swipeStack.Children.Add(BottomDock = new Dock
            {
                VerticalAlignment = EVerticalAlignment.Top,
                HorizontalAlignment = EHorizontalAlignment.Stretch,
                BackgroundColor = backgroundColor
            });

            // Make sure the bottom dock covers scrolling
            BottomDock.Children.Add(new UIElement
            {
                DesiredHeight = 15
            });

            HorizontalStack leftButtonStack = new HorizontalStack
            {
                HorizontalAlignment = EHorizontalAlignment.Stretch,
                VerticalAlignment = EVerticalAlignment.Bottom,
                Padding = new LayoutPadding(0, 5),
                ChildSpacing = 2
            };
        }

        public override void UpdateContentLayout()
        {
            UpdateColumnLayout();

            base.UpdateContentLayout();
        }

        public void SetItems(List<T> items)
        {
            this.items = items;

            ListDisplay.Items = items;
            ListDisplay.GoToFirstItem();
        }

        public void AddColumn(ItemDisplayColum<T> column)
        {
            DisplayColumns.Add(column);

            headerStack.Children.Add(new TextButton(column.DisplayName)
            {
                Padding = new LayoutPadding(1, 0),
                ClickAction = delegate {
                    SetSortColumn(column, toggleReverse: true);
                    Sort();
                }
            });

            UpdateColumnLayout();
        }

        public ItemDisplayColum<T> GetColumn(string displayOrPropertyName)
        {
            foreach (ItemDisplayColum<T> column in DisplayColumns)
            {
                if ((column.DisplayName == displayOrPropertyName) || (column.PropertyName == displayOrPropertyName))
                {
                    return column;
                }
            }

            return null;
        }

        void UpdateColumnLayout()
        {
            float widthRemaining = ContentBounds.Width - 20;
            int numDivideColumns = 0;

            foreach (ItemDisplayColum<T> column in DisplayColumns)
            {
                if (column.RequestedDisplayWidth == 0)
                {
                    numDivideColumns++;
                }
                else
                {
                    column.DisplayWidth = column.RequestedDisplayWidth + (ListDisplay.ItemXOffset * 2);
                    widthRemaining -= column.DisplayWidth;
                }
            }

            if (widthRemaining < 0)
                widthRemaining = 0;

            float divdeWidth = (numDivideColumns > 0) ? widthRemaining / (float)numDivideColumns : 0;

            float offset = 0;

            foreach (ItemDisplayColum<T> column in DisplayColumns)
            {
                if (column.RequestedDisplayWidth == 0)
                {
                    column.DisplayWidth = divdeWidth;
                }

                column.DisplayOffset = offset;
                offset += column.DisplayWidth;
            }

            int columnPos = 0;

            foreach (ItemDisplayColum<T> column in DisplayColumns)
            {
                headerStack.Children[columnPos].DesiredWidth = (int)(column.DisplayWidth - (headerStack.Children[columnPos].Padding.Left + headerStack.Children[columnPos].Padding.Right));

                columnPos++;
            }
        }

        public void SetSortColumn(string columnName)
        {
            SetSortColumn(GetColumn(columnName));
        }

        public void SetSortColumn(ItemDisplayColum<T> column)
        {
            SetSortColumn(column, toggleReverse: false);
        }

        public void SetSortColumn(ItemDisplayColum<T> column, bool toggleReverse)
        {
            if (toggleReverse && (column == CurrentSortColumn))
            {
                CurrentSortReverse = !CurrentSortReverse;
            }
            else
            {
                CurrentSortReverse = column.StartReversed;
            }

            CurrentSortColumn = column;
        }

        public void Sort()
        {
            if (CurrentSortColumn != null)
            {
                Sort(goToFirstItem: false);
            }
        }

        public void Sort(bool goToFirstItem)
        {
            T selectedItem = default;

            if (ListDisplay.LastSelectedItem != -1)
            {
                selectedItem = items[ListDisplay.LastSelectedItem];
            }

            if (CurrentSortReverse)
            {
                items.Sort(CurrentSortColumn.CompareReverse);
            }
            else
            {
                items.Sort(CurrentSortColumn.Compare);
            }

            if (selectedItem != null)
                ListDisplay.LastSelectedItem = items.IndexOf(selectedItem);

            if (goToFirstItem || (selectedItem == null))
            {
                ListDisplay.GoToFirstItem();
            }
            else
            {
                ListDisplay.SetTopItem(ListDisplay.LastSelectedItem);
            }
        }

        public int GetFirstMatchIndex(ReadOnlySpan<char> text)
        {
            if (CurrentSortColumn != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    object val = CurrentSortColumn.GetSortValue(items[i]);

                    if (val is string)
                    {
                        if (MemoryExtensions.StartsWith((val as string), text, StringComparison.InvariantCultureIgnoreCase))
                        {
                            return i;
                        }
                    }
                }
            }

            return -1;
        }

        public int GetIndexOf(T item)
        {
            Comparison<T> compare = CurrentSortReverse ? CurrentSortColumn.CompareReverse : CurrentSortColumn.Compare;

            for (int i = 0; i < items.Count; i++)
            {
                if (compare(item, items[i]) <= 0)
                {
                    return i;
                }
            }

            return -1;
        }

        char[] textMatch = new char[1];

        public override bool HandleTextInput(char c)
        {
            textMatch[0] = c;

            int index = GetFirstMatchIndex(textMatch);

            if (index != -1)
                ListDisplay.SetTopItem(index);

            return true;
        }

        public override void HandleInput(InputManager inputManager)
        {
            base.HandleInput(inputManager);

            int wheelDelta = inputManager.MouseWheelDelta;

            if (wheelDelta > 0)
            {
                ListDisplay.PreviousItem();
            }
            else if (wheelDelta < 0)
            {
                ListDisplay.NextItem();
            }

            if (inputManager.WasPressed("NextPage"))
            {
                ListDisplay.NextPage();
            }

            if (inputManager.WasPressed("PreviousPage"))
            {
                ListDisplay.PreviousPage();
            }

            if (inputManager.WasPressed("NextItem"))
            {
                ListDisplay.NextItem();
            }

            if (inputManager.WasPressed("PreviousItem"))
            {
                ListDisplay.PreviousItem();
            }

            if (inputManager.WasPressed("FirstItem"))
            {
                ListDisplay.GoToFirstItem();
            }

            if (inputManager.WasPressed("LastItem"))
            {
                ListDisplay.GoToLastItem();
            }
        }
    }
}
