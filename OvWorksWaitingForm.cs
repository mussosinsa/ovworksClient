using System;
using System.Drawing;
using System.Windows.Forms;

namespace csharpOvWorksClient_1._0._0
{
    /// <summary>시간이 걸리는 작업 동안 소유 폼 중앙에 표시되는 비모달 진행 안내 창.</summary>
    internal sealed class OvWorksWaitingForm : Form
    {
        private readonly Form ownerForm;

        public OvWorksWaitingForm(Form owner, string message)
        {
            if (owner == null) throw new ArgumentNullException("owner");
            ownerForm = owner;

            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.LightSkyBlue;
            ClientSize = new Size(420, 190);
            ControlBox = false;
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            Opacity = 0.97D;
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Text = "처리 중";
            UseWaitCursor = true;

            var title = new Label
            {
                BackColor = Color.FromArgb(73, 164, 222),
                Dock = DockStyle.Top,
                Font = new Font("돋움", 11.25F, FontStyle.Bold),
                ForeColor = Color.White,
                Height = 42,
                Text = "처리 중",
                TextAlign = ContentAlignment.MiddleCenter
            };
            var icon = new PictureBox
            {
                Image = Properties.Resources.circle_9360_128,
                Location = new Point(181, 53),
                Size = new Size(58, 58),
                SizeMode = PictureBoxSizeMode.Zoom,
                TabStop = false
            };
            var label = new Label
            {
                AutoEllipsis = true,
                Font = new Font("돋움", 11.25F, FontStyle.Bold),
                ForeColor = Color.FromArgb(18, 70, 105),
                Location = new Point(24, 119),
                Size = new Size(372, 52),
                Text = string.IsNullOrWhiteSpace(message) ? "처리 중입니다.\r\n잠시만 기다려 주십시오." : message,
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(title);
            Controls.Add(icon);
            Controls.Add(label);

            // CenterParent는 ShowDialog에서만 안정적으로 적용되므로 비모달 창의 화면 좌표를 계산한다.
            Location = new Point(
                owner.Left + Math.Max(0, (owner.Width - Width) / 2),
                owner.Top + Math.Max(0, (owner.Height - Height) / 2));

            owner.Enabled = false;
            try
            {
                Show(owner);
            }
            catch
            {
                owner.Enabled = true;
                throw;
            }
            BringToFront();
            Refresh();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var borderPen = new Pen(Color.FromArgb(73, 164, 222), 2F))
            {
                e.Graphics.DrawRectangle(borderPen, 1, 1, ClientSize.Width - 3, ClientSize.Height - 3);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && ownerForm != null && !ownerForm.IsDisposed)
            {
                ownerForm.Enabled = true;
                ownerForm.Activate();
            }
            base.Dispose(disposing);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 사용자가 Alt+F4로 진행 창만 닫지 못하게 하고, 작업을 수행하는 using 블록이
            // Dispose할 때에는 정상적으로 닫히게 한다.
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                return;
            }
            base.OnFormClosing(e);
        }
    }
}
