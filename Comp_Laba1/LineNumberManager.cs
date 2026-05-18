using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Comp_Laba1
{
    public class LineNumberManager
    {
        private RichTextBox textBox;
        private RichTextBox lineNumbers;
        private bool isUpdating = false;
        private Timer scrollSyncTimer;

        public LineNumberManager(RichTextBox mainTextBox, RichTextBox lineNumberBox)
        {
            textBox = mainTextBox;
            lineNumbers = lineNumberBox;

            SetupLineNumberBox();
            textBox.TextChanged += (s, e) => UpdateLineNumbers();
            textBox.VScroll += (s, e) => DelayedSyncScroll();
            textBox.Resize += (s, e) => UpdateLineNumbers();
            textBox.FontChanged += (s, e) => UpdateLineNumbers();
            scrollSyncTimer = new Timer();
            scrollSyncTimer.Interval = 50;
            scrollSyncTimer.Tick += (s, e) =>
            {
                scrollSyncTimer.Stop();
                SyncScroll();
            };

            UpdateLineNumbers();
        }

        private void SetupLineNumberBox()
        {
            lineNumbers.ReadOnly = true;
            lineNumbers.BackColor = Color.LightGray;
            lineNumbers.ForeColor = Color.Black;
            lineNumbers.WordWrap = false;
            lineNumbers.Text = "";
            lineNumbers.ShortcutsEnabled = false;
            lineNumbers.Multiline = true;
        }

        public void UpdateLineNumbers()
        {
            if (isUpdating || textBox == null || lineNumbers == null) return;

            try
            {
                isUpdating = true;

                int lineCount = textBox.Lines.Length;
                if (lineCount == 0) lineCount = 0;

                string numbers = "";
                for (int i = 0; i <= lineCount; i++)
                {
                    numbers += i.ToString() + "\n";
                }
                int scrollPos = GetScrollPos(textBox.Handle);

                lineNumbers.Font = new Font(textBox.Font.FontFamily,
                                             textBox.Font.Size,
                                             textBox.Font.Style);

                lineNumbers.Text = numbers;
                SetScrollPos(lineNumbers.Handle, scrollPos);

                AdjustWidth();
            }
            finally
            {
                isUpdating = false;
            }
        }

        private void DelayedSyncScroll()
        {
            scrollSyncTimer.Stop();
            scrollSyncTimer.Start();
        }

        private void SyncScroll()
        {
            if (isUpdating || textBox == null || lineNumbers == null) return;

            try
            {
                isUpdating = true;

                int scrollPos = GetScrollPos(textBox.Handle);
                SetScrollPos(lineNumbers.Handle, scrollPos);
            }
            finally
            {
                isUpdating = false;
            }
        }

        public void AdjustWidth()
        {
            if (lineNumbers == null) return;

            int maxLineNumber = textBox.Lines.Length;
            int digits = maxLineNumber.ToString().Length;
            if (digits < 3) digits = 3;

            int width = digits * (int)lineNumbers.Font.Size + 15;

            if (lineNumbers.Width != width)
            {
                lineNumbers.Width = width;
            }
        }

        [DllImport("user32.dll")]
        private static extern int GetScrollPos(IntPtr hWnd, int nBar);

        [DllImport("user32.dll")]
        private static extern int SetScrollPos(IntPtr hWnd, int nBar, int nPos, bool bRedraw);

        private const int SB_VERT = 1;

        private int GetScrollPos(IntPtr hWnd)
        {
            return GetScrollPos(hWnd, SB_VERT);
        }

        private void SetScrollPos(IntPtr hWnd, int pos)
        {
            SetScrollPos(hWnd, SB_VERT, pos, true);
        }
    }
}
