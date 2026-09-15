using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace csharpOvWorksClient_1._0._0
{
    /// <summary>테두리가 없는 폼을 Windows의 일반 제목 표시줄과 같은 방식으로 이동한다.</summary>
    internal static class BorderlessWindowDrag
    {
        private const int WmNcLButtonDown = 0x00A1;
        private const int HtCaption = 0x0002;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(
            IntPtr windowHandle, int message, IntPtr wParam, IntPtr lParam);

        public static void Begin(Form form, MouseEventArgs e)
        {
            if (form == null) throw new ArgumentNullException("form");
            if (e == null) throw new ArgumentNullException("e");
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            ReleaseCapture();
            SendMessage(form.Handle, WmNcLButtonDown, new IntPtr(HtCaption), IntPtr.Zero);
        }
    }
}
