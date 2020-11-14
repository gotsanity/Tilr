using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Tilr.Tray
{
    public partial class TilrMainForm : Form
    {
        private int _gridSizeX = 10;
        private int _gridSizeY = 10;
        private bool _isSelectingGrid = false;
        private int _selectionMinX, _selectionMinY, _selectionMaxX, _selectionMaxY;
        private List<Button> cells = new List<Button>();
        private IntPtr targetWindow;

        KeyboardHook hook = new KeyboardHook();

        public TilrMainForm()
        {
            InitializeComponent();

            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            trayIcon.Visible = true;
            trayContextMenu.Enabled = true;

            hook.KeyPressed += new EventHandler<KeyPressedEventArgs>(hook_KeyPressed);
            hook.RegisterHotKey(Tray.ModifierKeys.Control | Tray.ModifierKeys.Alt, Keys.Space);

            trayContextOpen.Click += new System.EventHandler(this.trayContextOpen_Click);
            trayContextExit.Click += new System.EventHandler(this.trayContextExit_Click);

            tableLayoutPanel.RowCount = _gridSizeY;
            tableLayoutPanel.ColumnCount = _gridSizeX;

            tableLayoutPanel.ColumnStyles.Clear();
            tableLayoutPanel.RowStyles.Clear();

            for (int i = 0; i < _gridSizeX; i++)
            {
                tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100 / _gridSizeX));
            }
            for (int i = 0; i < _gridSizeY; i++)
            {
                this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100 / _gridSizeY));
            }

            for (int y = 0; y < tableLayoutPanel.RowCount; y++)
            {
                for (int x = 0; x < tableLayoutPanel.ColumnCount; x++)
                {
                    var b = new Button();
                    //b.Text = string.Format("({0},{1})", x, y);
                    b.Name = string.Format("btnGridCell_{0}_{1}", x, y);
                    b.Dock = DockStyle.Fill;
                    b.Click += btnGridCell_Click;
                    b.BackColor = Color.LightGray;
                    b.MouseEnter += btnGridCell_MouseEnter;
                    tableLayoutPanel.Controls.Add(b, x, y);
                    
                    cells.Add(b);
                }
            }
        }

        void hook_KeyPressed(object sender, KeyPressedEventArgs e)
        {
            Debug.WriteLine(string.Format("Hotkey was pressed from {0}", GetActiveWindowTitle()));
            SetWindowSelection(GetActiveWindowTitle());
            openTilrForm();
        }

        private void btnGridCell_Click(object sender, EventArgs e)
        {
            var b = sender as Button;

            if (b == null)
            {
                return;
            }

            if (targetWindow == IntPtr.Zero)
            {
                MessageBox.Show("No window selected");
                return;
            }

            if (!_isSelectingGrid)
            {
                this._isSelectingGrid = true;
                _selectionMinX = tableLayoutPanel.GetColumn(b);
                _selectionMinY = tableLayoutPanel.GetRow(b);
                b.BackColor = Color.LightBlue;
            }
            else
            {
                _isSelectingGrid = false;
                _selectionMaxX = tableLayoutPanel.GetColumn(b);
                _selectionMaxY = tableLayoutPanel.GetRow(b);
                SetWindowToGridSelection();
            }
        }

        private void btnGridCell_MouseEnter(object sender, EventArgs e)
        {
            var b = sender as Button;
            
            if (b != null && _isSelectingGrid)
            {
                int x = tableLayoutPanel.GetColumn(b);
                int y = tableLayoutPanel.GetRow(b);

                int minX, minY, maxX, maxY;

                if (x <= _selectionMinX)
                {
                    minX = x;
                    maxX = _selectionMinX;
                } else
                {
                    maxX = x;
                    minX = _selectionMinX;
                }

                if (y <= _selectionMinY)
                {
                    minY = y;
                    maxY = _selectionMinY;
                }
                else
                {
                    maxY = y;
                    minY = _selectionMinY;
                }

                foreach (Button cell in cells)
                {
                    if (!isInRange(tableLayoutPanel.GetColumn(cell), minX, maxX) || !isInRange(tableLayoutPanel.GetRow(cell), minY, maxY))
                    {
                        cell.BackColor = Color.LightGray;
                    }
                    else
                    {
                        cell.BackColor = Color.LightBlue;
                    }
                }

                
            }
        }

        private bool isInRange(int test, int min, int max)
        {
            return test >= min && test <= max;
        }

        private void SetWindowToGridSelection()
        {
            Rectangle screenBounds = Screen.FromControl(this).Bounds;

            int cellSizeX = (screenBounds.Width / _gridSizeX);
            int cellSizeY = (screenBounds.Height / _gridSizeY);

            int posX, posY, posMaxX, posMaxY;
            int width, height;

            if (_selectionMinX <= _selectionMaxX)
            {
                posX = cellSizeX * _selectionMinX;
                posMaxX = cellSizeX * (_selectionMaxX + 1);
                width = posMaxX - posX;
            }
            else
            {
                posX = cellSizeX * _selectionMaxX;
                posMaxX = cellSizeX * (_selectionMinX + 1);
                width = posMaxX - posX;
            }

            if (_selectionMinY >= _selectionMaxY)
            {
                posY = cellSizeY * _selectionMaxY;
                posMaxY = cellSizeY * (_selectionMinY + 1);
                height = posMaxY - posY;
            }
            else
            {
                posY = cellSizeY * _selectionMinY;
                posMaxY = cellSizeY * (_selectionMaxY + 1);
                height = posMaxY - posY;
            }

            ResizeWindow(width, height, posX, posY);
        }

        private void TilrMainForm_SizeChanged(object sender, EventArgs e)
        {
            bool MouseOnTaskbar = Screen.GetWorkingArea(this).Contains(Cursor.Position);

            if (this.WindowState == FormWindowState.Minimized && MouseOnTaskbar)
            {
                trayIcon.Icon = SystemIcons.Application;
                trayIcon.BalloonTipText = "Tilr is running in the system tray.";
                trayIcon.ShowBalloonTip(1000);
                this.ShowInTaskbar = false;
                trayIcon.Visible = true;
                trayContextMenu.Enabled = true;
            }
        }

        private void openTilrForm()
        {
            this.WindowState = FormWindowState.Normal;

            if (this.WindowState == FormWindowState.Normal)
            {
                this.ShowInTaskbar = true;
                trayIcon.Visible = false;
                trayContextMenu.Hide();
            }
        }

        private void hideTilrForm()
        {
            this.WindowState = FormWindowState.Minimized;

            if (this.WindowState == FormWindowState.Minimized)
            {
                this.ShowInTaskbar = false;
                trayIcon.Visible = true;
                trayContextMenu.Enabled = true;
            }
        }

        private void trayContextMenu_Opening(object sender, CancelEventArgs e)
        {
            
        }

        private void trayContextOpen_Click(object sender, EventArgs e)
        {
            openTilrForm();
        }

        private void trayContextExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void trayIcon_Click(object sender, EventArgs e)
        {
            trayContextMenu.Show(Cursor.Position);
        }

        private void trayIcon_MouseClick(object sender, MouseEventArgs e)
        {
            trayContextMenu.Show(Cursor.Position);
        }

        private void SetWindowSelection(string caption)
        {
            targetWindow = FindWindowByCaption(IntPtr.Zero, caption);
            
            if (targetWindow == IntPtr.Zero)
            {
                MessageBox.Show(
                    "Could not find a window with the title \"" +
                    caption + "\"");
                return;
            }
        }

        private void ResizeWindow(int width, int height, int posX, int posY)
        {
            Debug.WriteLine(string.Format("Setting window position to {0},{1} with a width/height of {2}/{3}", posX, posY, width, height));
            SetWindowPos(targetWindow, IntPtr.Zero,
                posX, posY, width, height, 0);
        }

        // ****************************************************************************
        // Window resize functionality
        // ****************************************************************************


        // Define the FindWindow API function.
        [DllImport("user32.dll", EntryPoint = "FindWindow",
            SetLastError = true)]
        static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly,
            string lpWindowName);

        // Define the SetWindowPos API function.
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetWindowPos(IntPtr hWnd,
            IntPtr hWndInsertAfter, int X, int Y, int cx, int cy,
            SetWindowPosFlags uFlags);

        // Define the SetWindowPosFlags enumeration.
        [Flags()]
        private enum SetWindowPosFlags : uint
        {
            SynchronousWindowPosition = 0x4000,
            DeferErase = 0x2000,
            DrawFrame = 0x0020,
            FrameChanged = 0x0020,
            HideWindow = 0x0080,
            DoNotActivate = 0x0010,
            DoNotCopyBits = 0x0100,
            IgnoreMove = 0x0002,
            DoNotChangeOwnerZOrder = 0x0200,
            DoNotRedraw = 0x0008,
            DoNotReposition = 0x0200,
            DoNotSendChangingEvent = 0x0400,
            IgnoreResize = 0x0001,
            IgnoreZOrder = 0x0004,
            ShowWindow = 0x0040,
        }


        // Get active window on hotkey press
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        private string GetActiveWindowTitle()
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return null;
        }
    }
}
