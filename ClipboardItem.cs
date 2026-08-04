using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Fantasy.ClipboardHistory
{
    public class ClipboardItem : INotifyPropertyChanged
    {
        private string _text = string.Empty;
        private DateTime _timestamp;
        private bool _isPinned;

        public string Text
        {
            get => _text;
            set { if (_text != value) { _text = value; OnPropertyChanged(); OnPropertyChanged(nameof(Preview)); } }
        }

        public DateTime Timestamp
        {
            get => _timestamp;
            set { if (_timestamp != value) { _timestamp = value; OnPropertyChanged(); OnPropertyChanged(nameof(TimestampDisplay)); } }
        }

        public bool IsPinned
        {
            get => _isPinned;
            set { if (_isPinned != value) { _isPinned = value; OnPropertyChanged(); OnPropertyChanged(nameof(PinGlyph)); OnPropertyChanged(nameof(PinTooltip)); } }
        }

        [JsonIgnore]
        public string Preview
        {
            get
            {
                if (string.IsNullOrEmpty(_text)) return string.Empty;
                var single = _text.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
                while (single.Contains("  ")) single = single.Replace("  ", " ");
                single = single.Trim();
                if (single.Length > 100) single = single.Substring(0, 100) + "...";
                return single;
            }
        }

        [JsonIgnore]
        public string TimestampDisplay
        {
            get
            {
                var now = DateTime.Now;
                var diff = now - _timestamp;
                if (diff.TotalSeconds < 60) return "just now";
                if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
                if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
                if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
                return _timestamp.ToString("MMM d, HH:mm");
            }
        }

        [JsonIgnore]
        public string PinGlyph => _isPinned ? "★" : "☆";

        [JsonIgnore]
        public string PinTooltip => _isPinned ? "Unpin" : "Pin";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void RefreshTimestampDisplay() => OnPropertyChanged(nameof(TimestampDisplay));
    }
}
