using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace Fantasy.ClipboardHistory
{
    public partial class MainWindow : Window
    {
        private const int MaxItems = 100;

        private readonly ObservableCollection<ClipboardItem> _allItems = new();
        private readonly ObservableCollection<ClipboardItem> _filteredItems = new();

        private readonly ClipboardListener _clipboardListener = new();
        private readonly HotkeyManager _hotkeyManager = new();
        private readonly DispatcherTimer _timestampTimer;

        private bool _shuttingDown;
        private string _searchTerm = string.Empty;
        private string? _lastCapturedText;

        public MainWindow()
        {
            InitializeComponent();

            HistoryList.ItemsSource = _filteredItems;

            _timestampTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _timestampTimer.Tick += (_, _) =>
            {
                foreach (var item in _allItems) item.RefreshTimestampDisplay();
            };
            _timestampTimer.Start();
        }

        public void InitializeAfterStartup()
        {
            var pinned = Storage.LoadPinned();
            foreach (var p in pinned)
            {
                _allItems.Add(p);
            }
            RebuildFiltered();
            UpdateCountLabel();
            UpdateEmptyState();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var helper = new WindowInteropHelper(this);
            var source = HwndSource.FromHwnd(helper.EnsureHandle());
            source?.AddHook(WndProc);

            _clipboardListener.ClipboardChanged += OnClipboardChanged;
            _clipboardListener.Register(helper.Handle);

            _hotkeyManager.HotkeyPressed += (_, _) => Dispatcher.BeginInvoke(new Action(ShowAndActivate));
            _hotkeyManager.Register(helper.Handle, ModifierKeys.Control | ModifierKeys.Shift, Key.V);

            CaptureCurrentClipboardIfText();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            bool localHandled = false;
            _clipboardListener.HandleMessage(hwnd, msg, wParam, lParam, ref localHandled);
            if (localHandled) handled = true;

            bool hotkeyHandled = false;
            _hotkeyManager.HandleMessage(hwnd, msg, wParam, lParam, ref hotkeyHandled);
            if (hotkeyHandled) handled = true;

            return IntPtr.Zero;
        }

        private void OnClipboardChanged(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(CaptureCurrentClipboardIfText), DispatcherPriority.Background);
        }

        private void CaptureCurrentClipboardIfText()
        {
            try
            {
                if (!Clipboard.ContainsText()) return;
                string text = Clipboard.GetText();
                if (string.IsNullOrEmpty(text)) return;
                if (text == _lastCapturedText) return;
                _lastCapturedText = text;

                var existing = _allItems.FirstOrDefault(i => i.Text == text);
                if (existing != null)
                {
                    _allItems.Remove(existing);
                    existing.Timestamp = DateTime.Now;
                    InsertItemInOrder(existing);
                }
                else
                {
                    var item = new ClipboardItem
                    {
                        Text = text,
                        Timestamp = DateTime.Now,
                        IsPinned = false
                    };
                    InsertItemInOrder(item);
                    TrimUnpinned();
                }

                RebuildFiltered();
                UpdateCountLabel();
                UpdateEmptyState();
            }
            catch
            {
            }
        }

        private void InsertItemInOrder(ClipboardItem item)
        {
            if (item.IsPinned)
            {
                int index = 0;
                for (int i = 0; i < _allItems.Count; i++)
                {
                    if (_allItems[i].IsPinned) index = i + 1;
                    else break;
                }
                _allItems.Insert(index, item);
            }
            else
            {
                int index = 0;
                for (int i = 0; i < _allItems.Count; i++)
                {
                    if (_allItems[i].IsPinned) index = i + 1;
                    else break;
                }
                _allItems.Insert(index, item);
            }
        }

        private void TrimUnpinned()
        {
            var unpinned = _allItems.Where(i => !i.IsPinned).ToList();
            int excess = unpinned.Count - MaxItems;
            for (int i = 0; i < excess; i++)
            {
                var oldest = unpinned[unpinned.Count - 1 - i];
                _allItems.Remove(oldest);
            }
        }

        private void RebuildFiltered()
        {
            _filteredItems.Clear();
            IEnumerable<ClipboardItem> source = _allItems;
            if (!string.IsNullOrEmpty(_searchTerm))
            {
                var term = _searchTerm;
                source = source.Where(i => i.Text.Contains(term, StringComparison.OrdinalIgnoreCase));
            }
            var ordered = source
                .OrderByDescending(i => i.IsPinned)
                .ThenByDescending(i => i.Timestamp);
            foreach (var i in ordered) _filteredItems.Add(i);
        }

        private void UpdateCountLabel()
        {
            int total = _allItems.Count;
            int pinned = _allItems.Count(i => i.IsPinned);
            if (total == 0) CountLabel.Text = string.Empty;
            else if (pinned > 0) CountLabel.Text = $"· {total} items, {pinned} pinned";
            else CountLabel.Text = $"· {total} items";
        }

        private void UpdateEmptyState()
        {
            EmptyState.Visibility = _filteredItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        public void ShowAndActivate()
        {
            if (_shuttingDown) return;
            Visibility = Visibility.Visible;
            if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
            Show();
            Activate();
            Topmost = true;
            Topmost = false;
            Focus();
            SearchBox.Focus();
            SearchBox.SelectAll();
            RefreshAllTimestamps();
        }

        private void RefreshAllTimestamps()
        {
            foreach (var item in _allItems) item.RefreshTimestampDisplay();
        }

        public void HideWindow()
        {
            _searchTerm = string.Empty;
            SearchBox.Text = string.Empty;
            RebuildFiltered();
            UpdateEmptyState();
            Hide();
        }

        public void PrepareForShutdown()
        {
            _shuttingDown = true;
            Storage.SavePinned(_allItems);
            _clipboardListener.Dispose();
            _hotkeyManager.Dispose();
            _timestampTimer.Stop();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_shuttingDown)
            {
                e.Cancel = true;
                HideWindow();
                return;
            }
            base.OnClosing(e);
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                try { DragMove(); } catch { }
            }
        }

        private void HideButton_Click(object sender, RoutedEventArgs e) => HideWindow();

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            var unpinned = _allItems.Where(i => !i.IsPinned).ToList();
            foreach (var u in unpinned) _allItems.Remove(u);
            _lastCapturedText = null;
            RebuildFiltered();
            UpdateCountLabel();
            UpdateEmptyState();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchTerm = SearchBox.Text?.Trim() ?? string.Empty;
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;
            RebuildFiltered();
            UpdateEmptyState();
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (!string.IsNullOrEmpty(SearchBox.Text))
                {
                    SearchBox.Text = string.Empty;
                }
                else
                {
                    HideWindow();
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                if (_filteredItems.Count > 0)
                {
                    var first = _filteredItems[0];
                    CopyItemAndHide(first);
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                if (_filteredItems.Count > 0)
                {
                    HistoryList.Focus();
                    HistoryList.SelectedIndex = 0;
                    var container = HistoryList.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem;
                    container?.Focus();
                }
                e.Handled = true;
            }
        }

        private void HistoryList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (HistoryList.SelectedItem is ClipboardItem item)
            {
                CopyItemAndHide(item);
            }
        }

        private void HistoryList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && HistoryList.SelectedItem is ClipboardItem item)
            {
                CopyItemAndHide(item);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                HideWindow();
                e.Handled = true;
            }
            else if (e.Key == Key.Delete && HistoryList.SelectedItem is ClipboardItem del)
            {
                DeleteItem(del);
                e.Handled = true;
            }
        }

        private void CopyItemAndHide(ClipboardItem item)
        {
            try
            {
                _lastCapturedText = item.Text;
                Clipboard.SetText(item.Text);
                item.Timestamp = DateTime.Now;
                if (!item.IsPinned)
                {
                    _allItems.Remove(item);
                    InsertItemInOrder(item);
                }
                RebuildFiltered();
                HideWindow();
            }
            catch
            {
            }
        }

        private void PinButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is ClipboardItem item)
            {
                TogglePin(item);
            }
        }

        private void ContextCopy_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.DataContext is ClipboardItem item)
            {
                CopyItemAndHide(item);
            }
        }

        private void ContextPin_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.DataContext is ClipboardItem item)
            {
                TogglePin(item);
            }
        }

        private void ContextDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.DataContext is ClipboardItem item)
            {
                DeleteItem(item);
            }
        }

        private void TogglePin(ClipboardItem item)
        {
            item.IsPinned = !item.IsPinned;
            _allItems.Remove(item);
            InsertItemInOrder(item);
            Storage.SavePinned(_allItems);
            RebuildFiltered();
            UpdateCountLabel();
        }

        private void DeleteItem(ClipboardItem item)
        {
            _allItems.Remove(item);
            if (item.IsPinned) Storage.SavePinned(_allItems);
            RebuildFiltered();
            UpdateCountLabel();
            UpdateEmptyState();
        }
    }
}
