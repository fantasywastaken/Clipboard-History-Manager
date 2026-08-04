using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Fantasy.ClipboardHistory
{
    public class TrayIconManager : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly MainWindow _mainWindow;
        private Icon? _customIcon;

        public event EventHandler? ShowRequested;
        public event EventHandler? ExitRequested;

        public TrayIconManager(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;

            _customIcon = CreateTrayIcon();

            _notifyIcon = new NotifyIcon
            {
                Icon = _customIcon ?? SystemIcons.Application,
                Text = "Clipboard History Manager",
                Visible = true
            };

            var menu = new ContextMenuStrip
            {
                RenderMode = ToolStripRenderMode.System
            };

            var showItem = new ToolStripMenuItem("Show History");
            showItem.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            showItem.Click += (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty);

            var separator = new ToolStripSeparator();

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);

            menu.Items.Add(showItem);
            menu.Items.Add(separator);
            menu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = menu;
            _notifyIcon.DoubleClick += (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty);
        }

        private static Icon? CreateTrayIcon()
        {
            try
            {
                const int size = 32;
                using var bmp = new Bitmap(size, size);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.Clear(Color.Transparent);

                    using var bg = new SolidBrush(Color.FromArgb(255, 14, 99, 156));
                    using var path = RoundedRect(new Rectangle(2, 2, size - 4, size - 4), 6);
                    g.FillPath(bg, path);

                    using var whitePen = new Pen(Color.White, 2.5f)
                    {
                        StartCap = LineCap.Round,
                        EndCap = LineCap.Round
                    };
                    int left = 10, right = 22, top = 10;
                    g.DrawLine(whitePen, left, top, right, top);
                    g.DrawLine(whitePen, left, top + 6, right, top + 6);
                    g.DrawLine(whitePen, left, top + 12, right - 4, top + 12);
                }
                IntPtr hIcon = bmp.GetHicon();
                var icon = Icon.FromHandle(hIcon);
                return (Icon)icon.Clone();
            }
            catch
            {
                return null;
            }
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
            var path = new GraphicsPath();
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        public void ShowBalloon(string title, string message)
        {
            _notifyIcon.BalloonTipTitle = title;
            _notifyIcon.BalloonTipText = message;
            _notifyIcon.ShowBalloonTip(2000);
        }

        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _customIcon?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
