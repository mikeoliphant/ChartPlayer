using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UILayout;
using SongFormat;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Concurrent;

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
        BackgroundImage albumImage;
        TextBlock songTitleText;
        TextBlock songArtistText;
        TextBlock songAlbumText;
        TextToggleButton songFavoriteButton;
        HorizontalStack tagStack;
        HorizontalStack arrangementStack;
        DialogInputStack buttonInputStack;
        HorizontalStack filterStack;
        TextButton filterButton;
        NinePatchWrapper bottomInterface;
        string currentFilterTag = null;

        public SongListDisplay()
        {
            HorizontalAlignment = EHorizontalAlignment.Stretch;
            VerticalAlignment = EVerticalAlignment.Stretch;

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
                RequestedDisplayWidth = 80,
                SecondarySortColumn = artistColumn
            });
            SongList.AddColumn(new ItemDisplayColum<SongIndexEntry>
            {
                DisplayName = "Plays",
                ValueFunc = delegate (SongIndexEntry entry) { return (entry.Stats[(int)CurrentInstrument] == null) ? 0 : entry.Stats[(int)CurrentInstrument].NumPlays; },
                RequestedDisplayWidth = 70,
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

            HorizontalStack bottomStack = new HorizontalStack()
            {
                HorizontalAlignment = EHorizontalAlignment.Stretch
            };
            bottomInterface.Child = bottomStack;

            songDisplayStack = new HorizontalStack()
            {
                VerticalAlignment = EVerticalAlignment.Stretch,
                ChildSpacing = 10,
                Visible = false
            };
            bottomStack.Children.Add(songDisplayStack);

            albumImage = new BackgroundImage
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

            HorizontalStack favoriteStack = new HorizontalStack()
            {
                ChildSpacing = 2
            };
            songInfoStack.Children.Add(favoriteStack);

            favoriteStack.Children.Add(new TextBlock("Favorite:")
            {
                VerticalAlignment = EVerticalAlignment.Center
            });
            favoriteStack.Children.Add(songFavoriteButton = new TextToggleButton("Yes", "No")
            {
                VerticalAlignment = EVerticalAlignment.Center
            });

            tagStack = new HorizontalStack()
            {
                ChildSpacing = 2
            };
            songInfoStack.Children.Add(tagStack);

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
            buttonInputStack.AddInput(new DialogInput { Text = "Drums", Action = delegate { SetCurrentInstrument(ESongInstrumentType.Drums); } });
            buttonInputStack.AddInput(new DialogInput { Text = "Keys", Action = delegate { SetCurrentInstrument(ESongInstrumentType.Keys); } });
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

            bottomStack.Children.Add(new UIElement { HorizontalAlignment = EHorizontalAlignment.Stretch });

            filterStack = new HorizontalStack
            {
                HorizontalAlignment = EHorizontalAlignment.Right,
                VerticalAlignment = EVerticalAlignment.Top,
                Padding = new LayoutPadding(0, 5),
                ChildSpacing = 2
            };

            bottomStack.Children.Add(filterStack);

            filterStack.Children.Add(new TextBlock("Filter:")
            {
                VerticalAlignment = EVerticalAlignment.Center
            });

            filterStack.Children.Add(filterButton = new TextButton("All")
            {
                VerticalAlignment = EVerticalAlignment.Center
            });

            SongList.SetSortColumn("Artist");

            CloseAction = Close;
        }

        public virtual void Opened()
        {
        }

        public override bool HandleTouch(in Touch touch)
        {
            base.HandleTouch(touch);

            return true;
        }

        public override void HandleInput(InputManager inputManager)
        {
            base.HandleInput(inputManager);

            if (selectedSong != null)
            {
                if (inputManager.WasClicked("ToggleFavorite", this))
                {
                    SongStatsEntry stats = songIndex.GetSongStats(selectedSong, CurrentInstrument);

                    songFavoriteButton.Toggle();

                    if (songFavoriteButton.IsPressed)
                        selectedSong.Stats[(int)CurrentInstrument].AddTag("*");
                    else
                        selectedSong.Stats[(int)CurrentInstrument].RemoveTag("*");

                    UpdateSelectedSongDisplay();

                    songIndex.SaveStats();
                }

                if (inputManager.WasClicked("PlayCurrent", this))
                {
                    Play();
                }
            }
        }

        public void SetSongIndex(SongIndex songIndex)
        {
            this.songIndex = songIndex;

            this.allSongs = songIndex.Songs;

            SetCurrentSongs();

            UpdateTagFilter();
        }

        void SetCurrentSongs()
        {
            SongIndexEntry topSong = null;

            if ((allSongs == null) || (allSongs.Count == 0))
                return;

            if ((currentSongs != null) && (currentSongs.Count > 0))
                topSong = currentSongs[SongList.ListDisplay.CurrentTopItemIndex];

            IEnumerable<SongIndexEntry> songs = allSongs;

            switch (CurrentInstrument)
            {
                case ESongInstrumentType.LeadGuitar:
                    songs = songs.Where(s => s.Arrangements.Contains('L'));
                    break;
                case ESongInstrumentType.RhythmGuitar:
                    songs = songs.Where(s => s.Arrangements.Contains('R'));
                    break;
                case ESongInstrumentType.BassGuitar:
                    songs = songs.Where(s => s.Arrangements.Contains('B'));
                    break;
                case ESongInstrumentType.Drums:
                    songs = songs.Where(s => s.Arrangements.Contains('D'));
                    break;
                case ESongInstrumentType.Keys:
                    songs = songs.Where(s => s.Arrangements.Contains('K'));
                    break;
                case ESongInstrumentType.Vocals:
                    songs = songs.Where(s => s.Arrangements.Contains('V'));
                    break;
                default:
                    break;
            }

            if (currentFilterTag != null)
            {
                songs = songs.Where(s => s.HasTag(currentFilterTag, CurrentInstrument));
            }

            currentSongs = songs.ToList();

            SongList.SetItems(currentSongs);

            SongList.Sort();

            int songIndex = -1;

            if (selectedSong != null)
            {
                songIndex = SongList.GetIndexOf(selectedSong);
            }

            if (songIndex != -1)
            {
                SongList.ListDisplay.SetTopItem(songIndex);
                SongList.ListDisplay.LastSelectedItem = songIndex;
            }
            else
            {
                if (topSong != null)
                {
                    int topIndex = SongList.GetIndexOf(topSong);

                    SongList.ListDisplay.SetTopItem(topIndex);

                    SongList.ListDisplay.LastSelectedItem = -1;
                }
            }

            if (selectedSong != null)
                UpdateSelectedSongDisplay();
        }

        public void SetCurrentInstrument(ESongInstrumentType type)
        {
            if (CurrentInstrument != type)
            {
                CurrentInstrument = type;

                tuningColumn.PropertyName = type.ToString() + "Tuning";

                if (typeof(SongIndex).GetProperty(tuningColumn.PropertyName) == null)
                {
                    tuningColumn.PropertyName = "LeadGuitarTuning";
                }

                SetCurrentSongs();
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

        void UpdateTagFilter()
        {
            List<MenuItem> items = new List<MenuItem>();

            items.Add(new ContextMenuItem()
            {
                Text = "All",
                AfterCloseAction = delegate
                {
                    filterButton.Text = "All";
                    bottomInterface.UpdateContentLayout();

                    currentFilterTag = null;

                    SetCurrentSongs();
                }
            });

            foreach (string tag in songIndex.AllTags)
            {
                string text = tag;

                if (tag == "*")
                {
                    text = "Favorite";
                }

                items.Add(new ContextMenuItem()
                {
                    Text = text,
                    AfterCloseAction = delegate
                    {
                        filterButton.Text = text;
                        bottomInterface.UpdateContentLayout();

                        currentFilterTag = tag;

                        SetCurrentSongs();
                    }
                });
            }

            Menu menu = new Menu(items);

            filterButton.ClickAction = delegate
            {
                Layout.Current.ShowPopup(menu, filterButton.ContentBounds.Center);
            };
        }

        void UpdateSelectedSongDisplay()
        {
            albumImage.LoadImage(songIndex.GetAlbumPath(selectedSong));

            songTitleText.Text = selectedSong.SongName;
            songArtistText.Text = selectedSong.ArtistName;
            songAlbumText.Text = selectedSong.AlbumName;
            songFavoriteButton.SetPressed(selectedSong.HasTag("*", CurrentInstrument));
            songFavoriteButton.ClickAction = delegate
            {
                SongStatsEntry stats = songIndex.GetSongStats(selectedSong, CurrentInstrument);

                if (songFavoriteButton.IsPressed)
                    selectedSong.Stats[(int)CurrentInstrument].AddTag("*");
                else
                    selectedSong.Stats[(int)CurrentInstrument].RemoveTag("*");

                songIndex.SaveStats();
            };

            tagStack.Children.Clear();

            if (selectedSong.Stats[(int)CurrentInstrument] != null)
            {
                var tags = selectedSong.Stats[(int)CurrentInstrument].Tags;

                if (tags != null)
                {
                    foreach (string tag in tags)
                    {
                        if (tag != "*")
                        {
                            tagStack.Children.Add(new TextButton(tag)
                            {
                                ClickAction = delegate
                                {
                                    Layout.Current.ShowConfirmationPopup("Remove \"" + tag + "\" tag?", delegate
                                    {
                                        SongStatsEntry stats = songIndex.GetSongStats(selectedSong, CurrentInstrument);

                                        stats.RemoveTag(tag);

                                        songIndex.SaveStats();

                                        UpdateSelectedSongDisplay();
                                    });
                                }
                            });
                        }
                    }
                }
            }

            tagStack.Children.Add(new TextButton("+Tag")
            {
                ClickAction = AddTag
            });

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

        void AddTag()
        {
            List<MenuItem> items = new List<MenuItem>();

            foreach (string tag in songIndex.AllTags)
            {
                if (tag == "*")
                    continue;

                items.Add(new ContextMenuItem()
                {
                    Text = tag,
                    AfterCloseAction = delegate
                    {
                        SongStatsEntry stats = songIndex.GetSongStats(selectedSong, CurrentInstrument);

                        stats.AddTag(tag);

                        songIndex.SaveStats();

                        UpdateSelectedSongDisplay();
                    }
                });
            }

            items.Add(new ContextMenuItem()
            {
                Text = "New Tag",
                AfterCloseAction = delegate
                {
                    Layout.Current.ShowTextInputPopup("New Tag:", "", delegate (string text)
                    {
                        if (!string.IsNullOrEmpty(text))
                        {
                            text = Regex.Replace(text, "[^A-Za-z0-9 -_]", "");

                            songIndex.AddTag(text);

                            SongStatsEntry stats = songIndex.GetSongStats(selectedSong, CurrentInstrument);

                            stats.AddTag(text);

                            songIndex.SaveStats();

                            UpdateSelectedSongDisplay();
                        }
                    }, UIColor.Black, new UIColor(200, 200, 200));
                }
            });

            items.Add(new ContextMenuItem()
            {
                Text = "Cancel"
            });

            Layout.Current.ShowPopup(new Menu(items), tagStack.ContentBounds.Center);
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
                if (ListDisplay.LastSelectedItem < items.Count)
                {
                    selectedItem = items[ListDisplay.LastSelectedItem];
                }
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

        char[] textMatch = new char[100];
        int textMatchCharPos = 0;
        DateTime lastCharTime = DateTime.MinValue;

        public override bool HandleTextInput(char c)
        {
            double secsElapsed = (DateTime.Now - lastCharTime).TotalSeconds;

            if (secsElapsed > 1)
            {
                textMatchCharPos = 0;
            }

            textMatch[textMatchCharPos] = c;

            if (textMatchCharPos < (textMatch.Length - 1))
                textMatchCharPos++;

            int index = GetFirstMatchIndex(new ReadOnlySpan<char>(textMatch, 0, textMatchCharPos));

            if (index != -1)
                ListDisplay.SetTopItem(index);

            lastCharTime = DateTime.Now;

            return true;
        }

        public override void HandleInput(InputManager inputManager)
        {
            base.HandleInput(inputManager);

            int wheelDelta = inputManager.MouseWheelDelta;

            //if (wheelDelta > 0)
            //{
            //    ListDisplay.ScrollBackward();
            //}
            //else if (wheelDelta < 0)
            //{
            //    ListDisplay.ScrollForward();
            //}

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

    public class BackgroundImage : ImageElement
    {
        ConcurrentQueue<string> loadQueue = new ConcurrentQueue<string>();

        Thread loadThread = null;
        UIImage toDelete = null;

        public BackgroundImage()
            : base("SingleWhitePixel")
        {

        }

        public void LoadImage(string imagePath)
        {
            loadQueue.Enqueue(imagePath);

            if (loadThread != null)
            {
                if (!loadThread.IsAlive)
                {
                    loadThread.Join();

                    loadThread = null;
                }
            }

            if (loadThread == null)
            {
                loadThread = new Thread(new ThreadStart(BackgroundLoad));
                loadThread.Start();
            }
        }

        public void ClearImage()
        {
            LoadImage(null);
        }

        void Delete()
        {
            if ((toDelete != null) && (toDelete.Width > 1))
            {
                if (toDelete.Texture != null)
                {
                    toDelete.Texture.Dispose();
                }
            }

            toDelete = null;
        }

        void BackgroundLoad()
        {
            string toLoad = null;

            if (loadQueue.Count > 0)
            {
                string tryLoad;

                while (loadQueue.TryDequeue(out tryLoad))
                {
                    toLoad = tryLoad;
                }

                Delete();

                if (toLoad == null)
                {
                    toDelete = Image;

                    Image = Layout.Current.GetImage("SingleWhitePixel");
                }
                else
                {
                    UIImage loadImage = null;

                    try
                    {
                        Delete();

                        using (Stream inputStream = File.OpenRead(toLoad))
                        {
                            loadImage = new UIImage(inputStream);

                            toDelete = Image;

                            Image = loadImage;
                        }
                    }
                    catch
                    {
                        toDelete = Image;

                        Image = Layout.Current.GetImage("SingleWhitePixel");
                    }
                }
            }
        }
    }
}
