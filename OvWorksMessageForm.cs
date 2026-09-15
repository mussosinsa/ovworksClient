using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace csharpOvWorksClient_1._0._0
{
    public partial class OvWorksMessageForm : Form
    {
        public OvWorksMessageForm()
        {
            InitializeComponent();
        }
        public OvWorksMessageForm(string str)
        {
            InitializeComponent();
            messageLabel.Text = str;
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            this.Close();
            OvWorksLoginForm loginForm = new OvWorksLoginForm();
            loginForm.ShowDialog();
        }
    }
}
