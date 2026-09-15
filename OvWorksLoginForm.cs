using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace csharpOvWorksClient_1._0._0
{
    public partial class OvWorksLoginForm : Form
    {
        // Data Integrity
        OvWorksHash ovWorksHash;
        public OvWorksLoginForm()
        {
            InitializeComponent();

            // Data Integrity Check 
            ovWorksHash = new OvWorksHash();
            //hashCheck();
            //ovWorksHash.integrityCheck();            
            if (!integritySW.Expired)
            {
                // 시작할 때는 결과 팝업을 띄우지 않는다. 실패 상태는 감사 이벤트에 전달되고,
                // 사용자가 무결성 검사 버튼을 누르면 상세 결과를 확인할 수 있다.
                ovWorksHash.integrityCheck(false);
            }

            // 테두리가 없는 로그인 창도 일반 제목 표시줄처럼 이동할 수 있게 한다.
            // 입력 컨트롤과 버튼에는 연결하지 않아 텍스트 선택/클릭 동작을 방해하지 않는다.
            panel2.MouseDown += WindowDrag_MouseDown;
            pictureBox1.MouseDown += WindowDrag_MouseDown;
            pictureBox2.MouseDown += WindowDrag_MouseDown;
            label1.MouseDown += WindowDrag_MouseDown;
        }

        private void WindowDrag_MouseDown(object sender, MouseEventArgs e)
        {
            BorderlessWindowDrag.Begin(this, e);
        }

        /*
        private void hashCheck()
        {
            //usernameTextBox.Text = DateTime.Now.ToString();
            ovWorksHash.hash512Value = ovWorksHash.hashFileReader(ovWorksHash.hashFileName);
            byte[] targetHash512Value = ovWorksHash.GetHashSha512(ovWorksHash.targetFileName);
            //bool check= ovWorksHash.hash512Value.SequenceEqual(targetHash512Value);
            if (ovWorksHash.hash512Value.SequenceEqual(targetHash512Value) != true)
            {
                //MessageBox.Show($"해시:{ovWorksHash.BytesToString(ovWorksHash.hash512Value)}, {ovWorksHash.hash512Value.Length}\n" +
                //    $"타켓:{ovWorksHash.BytesToString(targetHash512Value)}, {targetHash512Value.Length}", "Failure", MessageBoxButtons.OK,MessageBoxIcon.Warning);
                MessageBox.Show("무결성 검사 결과 실패하여 프로그램을 실행할 수 없습니다.", "무결성 검사 - 실패(Failure)", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                //Environment.Exit(0);
                ovWorksHash.hashStatus = "error";
            }
            else
            {
                ovWorksHash.hashStatus = "normal";
            }
        }
        */
        // ========================================================================================================================================
        public Boolean EmptyCheck()
        {
            if (string.IsNullOrEmpty(serialNumberTextBox.Text))
            {
                MessageBox.Show("시리얼코드(Serial Number)가 입력되지 않았습니다. \n정확하게 입력 바랍니다.", "시리얼코드(Serial Number) 입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return true;
            }
            if (string.IsNullOrEmpty(hostTextBox.Text))
            {
                MessageBox.Show("호스트(Host)가 입력되지 않았습니다. \n정확하게 입력 바랍니다.","호스트(Host) 입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return true;
            }
            if (string.IsNullOrEmpty(usernameTextBox.Text))
            {
                MessageBox.Show("사용자 계정(Username)가 입력되지 않았습니다. \n정확하게 입력 바랍니다.", "사용자 계정(Username) 입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return true;
            }
            if (string.IsNullOrEmpty(profileTextBox.Text))
            {
                MessageBox.Show("프로필(Profile)가 입력되지 않았습니다. \n정확하게 입력 바랍니다.", "프로필(Profile) 입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return true;
            }
            if (string.IsNullOrEmpty(passwordTextBox.Text))
            {
                MessageBox.Show("비밀번호(Password)가 입력되지 않았습니다. \n정확하게 입력 바랍니다.", "비밀번호(Password) 입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return true;
            }
            return false;
        }
        public void ClearInfo()
        {
            hostTextBox.Text = string.Empty;
            usernameTextBox.Text = string.Empty;
            profileTextBox.Text = string.Empty;
            passwordTextBox.Text = string.Empty;
        }
        private void quitButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void eyePictureBox_Click(object sender, EventArgs e)
        {
            passwordTextBox.UseSystemPasswordChar = false;
        }

        private void eyePictureBox_MouseLeave(object sender, EventArgs e)
        {
            passwordTextBox.UseSystemPasswordChar=true;
        }

        private void loginButton_Click(object sender, EventArgs e)
        {
            try
            {
                string publicKeyPath = OvWorksApplicationFiles.PublicKeyPath;

                if (!File.Exists(publicKeyPath))
                {
                    MessageBox.Show("공개키 파일이 없습니다.\r\n\r\n" + publicKeyPath);

                    return;
                }

                string username = usernameTextBox.Text;
                string password = passwordTextBox.Text;

                if (!EmptyCheck())
                {
                    // 여기서 암호화하는 것은 공개키가 쓸 수 있는 상태인지 지금 확인해 두기 위해서다.
                    // 실제로 보낼 자격증명은 대시보드가 인증할 때마다 새로 만든다. 한 번 만든
                    // 암호문을 들고 다니면 그것을 가로챈 쪽이 나중에 그대로 다시 보낼 수 있고,
                    // 그것이 이 화면이 평문을 넘기게 된 이유다.
                    RsaEncryptionLegacy.EncryptWithPemPublicKey(publicKeyPath, username);
                    // 실제 패킷과 동일하게 envelope까지 포함한 길이와 공개키 상태를 확인한다.
                    RsaEncryptionLegacy.EncryptWithPemPublicKey(
                        publicKeyPath, OvWorksLoginEnvelope.Wrap(password));

                    using (OvWorksClientDashboardForm ovWorksClientDashboardForm = new OvWorksClientDashboardForm(serialNumberTextBox.Text, hostTextBox.Text, usernameTextBox.Text, username, profileTextBox.Text, password, ovWorksHash.hashStatus))
                    {
                        this.Hide();
                        try
                        {
                            ovWorksClientDashboardForm.ShowDialog();
                        }
                        finally
                        {
                            passwordTextBox.Clear();
                            this.Show();
                            this.Activate();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "로그인 초기화 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void hostTextBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            hostTextBox.Text = string.Empty;
        }

        private void usernameTextBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            usernameTextBox.Text = string.Empty;
        }

        private void profileTextBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            profileTextBox.Text = string.Empty;
        }

        private void passwordTextBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            passwordTextBox.Text = string.Empty;
        }

        private void serialNumberTextBox_Leave(object sender, EventArgs e)
        {
            if (serialNumberTextBox.Text == "")
            {
                serialNumberTextBox.Text = "SerialNumber";
                serialNumberTextBox.ForeColor = Color.Silver;
            }
        }

        private void serialNumberTextBox_Enter(object sender, EventArgs e)
        {
            if (serialNumberTextBox.Text == "SerialNumber")
            {
                serialNumberTextBox.Text = "";
                serialNumberTextBox.ForeColor = Color.Black;
            }
        }

        private void hostTextBox_Leave(object sender, EventArgs e)
        {
            if(hostTextBox.Text == "")
            {
                hostTextBox.Text = "manager.domain.com";
                hostTextBox.ForeColor = Color.Silver;
            }
        }

        private void hostTextBox_Enter(object sender, EventArgs e)
        {
            if (hostTextBox.Text == "manager.domain.com")
            {
                hostTextBox.Text = "";
                hostTextBox.ForeColor = Color.Black;
            }
        }
    }
}
