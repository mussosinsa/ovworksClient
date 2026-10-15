using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace csharpOvWorksClient_1._0._0
{
    internal class OvWorksWaitingForm : Form
    {
        public OvWorksWaitingForm(Form owner)
        {
            Owner = owner;

            // Size this form to cover the main form's client rectangle.
            Size = Owner.ClientRectangle.Size;
            FormBorderStyle = FormBorderStyle.None;
            UseWaitCursor = true;
            StartPosition = FormStartPosition.Manual;
            Location = PointToClient(
                Owner.PointToScreen(Owner.ClientRectangle.Location));

            // Add a label that says "Loading..."
            var label = new Label
            {
                Image = Properties.Resources.circle_9360_128,
                ImageAlign = ContentAlignment.MiddleCenter,
                //Text = "Loading...",
                //TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
            };
            Controls.Add(label);

            // Set the form color to see-through Blue
            BackColor = Color.LightBlue;
            Opacity = .7;

            var forceHandle = Handle;
            //BeginInvoke(() => ShowDialog());
            this.BeginInvoke(new Action(() => { ShowDialog(); }));
        }
    }
}
