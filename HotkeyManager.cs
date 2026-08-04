using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Fantasy.ClipboardHistory
{
    public class HotkeyManager : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public const int HotkeyId = 0xC001;
        public const int WmHotkey = 0x0312;

        private const uint ModAlt = 0x0001;
        private const uint ModControl = 0x0002;
        private const uint ModShift = 0x0004;
        private const uint ModWin = 0x0008;

        private IntPtr _hwnd;
        private bool _registered;

        public event EventHandler? HotkeyPressed;

        public bool Register(IntPtr hwnd, ModifierKeys modifiers, Key key)
        {
            _hwnd = hwnd;
            uint mods = 0;
            if ((modifiers & ModifierKeys.Alt) == ModifierKeys.Alt) mods |= ModAlt;
            if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control) mods |= ModControl;
            if ((modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) mods |= ModShift;
            if ((modifiers & ModifierKeys.Windows) == ModifierKeys.Windows) mods |= ModWin;

            uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);
            _registered = RegisterHotKey(hwnd, HotkeyId, mods, vk);
            return _registered;
        }

        public IntPtr HandleMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
            {
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
                handled = true;
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            if (_registered && _hwnd != IntPtr.Zero)
            {
                UnregisterHotKey(_hwnd, HotkeyId);
                _registered = false;
            }
            GC.SuppressFinalize(this);
        }
    }
}
