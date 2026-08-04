using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Fantasy.ClipboardHistory
{
    public static class Storage
    {
        private static readonly string AppFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ClipboardHistory");

        private static readonly string StorePath = Path.Combine(AppFolder, "store.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static List<ClipboardItem> LoadPinned()
        {
            try
            {
                if (!File.Exists(StorePath)) return new List<ClipboardItem>();
                var json = File.ReadAllText(StorePath);
                if (string.IsNullOrWhiteSpace(json)) return new List<ClipboardItem>();
                var items = JsonSerializer.Deserialize<List<ClipboardItem>>(json, JsonOptions);
                return items ?? new List<ClipboardItem>();
            }
            catch
            {
                return new List<ClipboardItem>();
            }
        }

        public static void SavePinned(IEnumerable<ClipboardItem> items)
        {
            try
            {
                if (!Directory.Exists(AppFolder)) Directory.CreateDirectory(AppFolder);
                var list = new List<ClipboardItem>();
                foreach (var i in items)
                {
                    if (i.IsPinned) list.Add(i);
                }
                var json = JsonSerializer.Serialize(list, JsonOptions);
                File.WriteAllText(StorePath, json);
            }
            catch
            {
            }
        }
    }
}
