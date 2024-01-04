using System;
using System.Collections.Generic;
using System.Linq;
using UILayout;
using SongFormat;

namespace BassJam
{
    public class SongListDisplay : MultiColumnItemDisplay<SongIndexEntry>
    {
        public ESongInstrumentType CurrentInstrument { get; private set; } = ESongInstrumentType.LeadGuitar;

        List<SongIndexEntry> allSongs;
        List<SongIndexEntry> currentSongs;
        ItemDisplayColum<SongIndexEntry> tuningColumn;

        public SongListDisplay()
        {
            Padding = new LayoutPadding(5);

            BackgroundColor = UIColor.Black;

            ListDisplay.SelectAction = SongSelected;

            AddColumn(new ItemDisplayColum<SongIndexEntry> { DisplayName = "Title", PropertyName = "SongName" });
            AddColumn(new ItemDisplayColum<SongIndexEntry> { DisplayName = "Artist", PropertyName = "ArtistName" });

            float width = 0;
            float height = 0;

            ListDisplay.Font.SpriteFont.MeasureString("Eb Drop Db", out width, out height);
            AddColumn(tuningColumn = new ItemDisplayColum<SongIndexEntry> { DisplayName = "Tuning", PropertyName = "BassGuitarTuning", RequestedDisplayWidth = width });

            LeftInputStack.AddInput(new DialogInput { Text = "Lead", Action = delegate { SetCurrentInstrument(ESongInstrumentType.LeadGuitar); } });
            LeftInputStack.AddInput(new DialogInput { Text = "Rhythm", Action = delegate { SetCurrentInstrument(ESongInstrumentType.RhythmGuitar); } });
            LeftInputStack.AddInput(new DialogInput { Text = "Bass", Action = delegate { SetCurrentInstrument(ESongInstrumentType.BassGuitar); } });
            LeftInputStack.AddInput(new DialogInput { Text = "Keys", Action = delegate { SetCurrentInstrument(ESongInstrumentType.Keys); } });
            LeftInputStack.AddInput(new DialogInput { Text = "Close", Action = Close });

            SetSortColumn("Title");
        }

        public void SetSongs(List<SongIndexEntry> songs)
        {
            this.allSongs = songs;

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

            SetItems(currentSongs);

            Sort(toggleReverse: false);
        }

        public void SetCurrentInstrument(ESongInstrumentType type)
        {
            if (CurrentInstrument != type)
            {
                tuningColumn.PropertyName = type.ToString() + "Tuning";

                if (CurrentInstrument != type)
                {
                    CurrentInstrument = type;

                    SetCurrentSongs();
                }
            }
        }


        public void SongSelected(int index)
        {
            Close();

            SongPlayerInterface.Instance.SetSong(currentSongs[index], CurrentInstrument);
        }

        public void Close()
        {
            Layout.Current.ClosePopup(this);
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
        public string SortPropertyName { get; set; }
        public bool StartReversed { get; set; }
        public string DisplayName { get; set; }
        public float DisplayOffset { get; set; }
        public float DisplayWidth { get; set; }
        public float RequestedDisplayWidth { get; set; }

        public object GetSortValue(T obj)
        {
            return obj.GetType().GetProperty(SortPropertyName ?? PropertyName).GetValue(obj, null);
        }

        public object GetValue(T obj)
        {
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

            return ((IComparable)aObj).CompareTo((IComparable)bObj);
        }

        public int CompareReverse(T a, T b)
        {
            object aObj = GetSortValue(b);
            object bObj = GetSortValue(a);

            if (aObj == null)
            {
                if (bObj == null)
                    return 0;

                return 1;
            }

            if (bObj == null)
                return -1;

            return ((IComparable)aObj).CompareTo((IComparable)bObj);
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

    public class MultiColumnItemDisplay<T> : Dock, IPopup
    {
        public MultiColumnItemDisplaySwipeList<T> ListDisplay { get; private set; }
        internal List<ItemDisplayColum<T>> DisplayColumns { get; private set; }
        public DialogInputStack LeftInputStack { get; private set; }
        public DialogInputStack RightInputStack { get; private set; }
        public Action CloseAction { get; set; }

        List<T> items;
        Dock topDock;
        Dock bottomDock;
        HorizontalStack headerStack;
        ItemDisplayColum<T> lastSortColumn;
        bool lastSortReverse;

        public MultiColumnItemDisplay()
        {
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
                HorizontalAlignment = EHorizontalAlignment.Stretch
            });

            headerStack = new HorizontalStack { HorizontalAlignment = EHorizontalAlignment.Stretch };
            topDock.Children.Add(headerStack);

            ListDisplay = new MultiColumnItemDisplaySwipeList<T>(this);
            ListDisplay.HorizontalAlignment = EHorizontalAlignment.Stretch;
            ListDisplay.VerticalAlignment = EVerticalAlignment.Stretch;
            swipeStack.Children.Add(ListDisplay);
            swipeStack.DrawBehindElement = ListDisplay;

            swipeStack.Children.Add(bottomDock = new Dock
            {
                VerticalAlignment = EVerticalAlignment.Top,
                HorizontalAlignment = EHorizontalAlignment.Stretch
            });

            // Make sure the bottom dock covers scrolling
            bottomDock.Children.Add(new UIElement { DesiredHeight = 15 });

            HorizontalStack leftButtonStack = new HorizontalStack
            {
                HorizontalAlignment = EHorizontalAlignment.Stretch,
                VerticalAlignment = EVerticalAlignment.Bottom,
                Padding = new LayoutPadding(0, 5),
                ChildSpacing = 2
            };

            LeftInputStack = new DialogInputStack(this, leftButtonStack);
            bottomDock.Children.Add(LeftInputStack);

            HorizontalStack rightButtonStack = new HorizontalStack
            {
                HorizontalAlignment = EHorizontalAlignment.Stretch,
                VerticalAlignment = EVerticalAlignment.Bottom,
                Padding = new LayoutPadding(0, 5),
                ChildSpacing = 2
            };

            RightInputStack = new DialogInputStack(this, rightButtonStack) { HorizontalAlignment = EHorizontalAlignment.Right };
            bottomDock.Children.Add(RightInputStack);
        }

        public virtual void Opened()
        {
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
            ListDisplay.FirstItem();
        }

        public void AddColumn(ItemDisplayColum<T> column)
        {
            DisplayColumns.Add(column);

            headerStack.Children.Add(new TextButton(column.DisplayName)
            {
                Padding = new LayoutPadding(1, 0),
                ClickAction = delegate {
                    Sort(column, toggleReverse: true);
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
            float widthRemaining = ContentBounds.Width;
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
                headerStack.Children[columnPos].DesiredWidth = column.DisplayWidth;

                columnPos++;
            }
        }

        public void SetSortColumn(string columnName)
        {
            lastSortColumn = GetColumn(columnName);
        }

        public void SetSortColumn(ItemDisplayColum<T> column)
        {
            lastSortColumn = column;
        }

        public void Sort(bool toggleReverse)
        {
            if (lastSortColumn != null)
            {
                Sort(lastSortColumn, toggleReverse);
            }
        }

        public void Sort(string columnName, bool toggleReverse)
        {
            ItemDisplayColum<T> column = GetColumn(columnName);

            if (column != null)
            {
                Sort(column, toggleReverse);
            }
        }

        public void Sort(ItemDisplayColum<T> column, bool toggleReverse)
        {
            if (toggleReverse && (column == lastSortColumn))
            {
                lastSortReverse = !lastSortReverse;
            }
            else
            {
                lastSortReverse = column.StartReversed;
            }

            lastSortColumn = column;

            if (lastSortReverse)
            {
                items.Sort(column.CompareReverse);
            }
            else
            {
                items.Sort(column.Compare);
            }

            ListDisplay.FirstItem();
        }

        public void Randomize()
        {
            //PixGame.Random.RandomizeList(items);

            throw new NotImplementedException();

            ListDisplay.FirstItem();
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

            foreach (ItemDisplayColum<T> column in DisplayColumns)
            {
                if (inputManager.WasClicked(column.DisplayName, this))
                {
                    Sort(column, toggleReverse: true);

                    break;
                }
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
                ListDisplay.FirstItem();
            }

            if (inputManager.WasPressed("LastItem"))
            {
                ListDisplay.LastItem();
            }
        }
    }
}
