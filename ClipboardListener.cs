using System;
using System.Runtime.InteropServices;

namespace Fantasy.ClipboardHistory
{
    public class ClipboardListener : IDisposable
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        public const int WmClipboardUpdate = 0x031D;

        private IntPtr _hwnd;
        private bool _registered;

        public event EventHandler? ClipboardChanged;

        public bool Register(IntPtr hwnd)
        {
            _hwnd = hwnd;
            _registered = AddClipboardFormatListener(hwnd);
            return _registered;
        }

        public IntPtr HandleMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WmClipboardUpdate)
            {
                ClipboardChanged?.Invoke(this, EventArgs.Empty);
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            if (_registered && _hwnd != IntPtr.Zero)
            {
                RemoveClipboardFormatListener(_hwnd);
                _registered = false;
            }
            GC.SuppressFinalize(this);
        }
    }
}
