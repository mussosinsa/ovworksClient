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

            // Program.Main의 시작 검사를 통과한 상태를 대시보드 감사 이벤트에 전달한다.
            ovWorksHash = new OvWorksHash();

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

        private async void loginButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (EmptyCheck())
                {
                    return;
                }

                string publicKeyPath = OvWorksApplicationFiles.PublicKeyPath;
                string caCertificatePath = OvWorksApplicationFiles.CaCertificatePath;
                bool caCertificateMissing = !File.Exists(caCertificatePath);
                bool publicKeyMissing = !File.Exists(publicKeyPath);

                if (caCertificateMissing || publicKeyMissing)
                {
                    string missingFiles =
                        (caCertificateMissing ? "- ca.crt\r\n" : string.Empty) +
                        (publicKeyMissing ? "- public_key.pem\r\n" : string.Empty);
                    if (!OvWorksProductInfo.DemoSecurityBootstrap)
                    {
                        MessageBox.Show(
                            "운영 모드에서는 보안 파일을 네트워크로 자동 다운로드하지 않습니다.\r\n\r\n" +
                            missingFiles + "\r\n관리자가 검증한 파일을 다음 폴더에 직접 등록해 주세요.\r\n" +
                            OvWorksApplicationFiles.SecurityFilesDirectory,
                            "보안 파일 직접 등록 필요", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        return;
                    }

                    DialogResult downloadResult = MessageBox.Show(
                        "로그인에 필요한 다음 보안 파일이 없습니다.\r\n\r\n" +
                        missingFiles + "\r\n사용자 보안 파일 폴더에 다운로드하시겠습니까?\r\n" +
                        OvWorksApplicationFiles.SecurityFilesDirectory,
                        "데모 전용 보안 파일 다운로드",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (downloadResult != DialogResult.Yes)
                    {
                        return;
                    }

                    loginButton.Enabled = false;
                    try
                    {
                        await OvWorksSecurityFilesDownloader.DownloadMissingFilesAsync(
                            hostTextBox.Text);
                    }
                    finally
                    {
                        loginButton.Enabled = true;
                    }

                    MessageBox.Show(
                        "보안 파일 다운로드가 완료되었습니다.",
                        "보안 파일 다운로드",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                // 정적 파일 시작 검사를 통과한 뒤 보안 파일만 별도 단계로 검증한다.
                bool securityFilesValid = OvWorksProductInfo.DemoSecurityBootstrap
                    ? ovWorksHash.demoSecurityFilesCheck()
                    : ovWorksHash.integrityCheck(false, true);
                if (!securityFilesValid)
                {
                    MessageBox.Show(
                        "보안 파일 준비 후 무결성 검사에 실패했습니다.\r\n\r\n" +
                        ovWorksHash.LastFailureDetail,
                        "무결성 검사 실패",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Stop);
                    return;
                }

                string username = usernameTextBox.Text;
                string password = passwordTextBox.Text;

                OvWorksProtectedValue protectedUsername = null;
                OvWorksProtectedValue protectedPassword = null;
                byte[] usernameBytes = null;
                byte[] passwordBytes = null;
                try
                {
                    usernameBytes = System.Text.Encoding.UTF8.GetBytes(username);
                    passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
                    // 공개키와 실제 envelope 크기를 바이트 기반으로 확인하여 추가 평문 string을
                    // 만들지 않는다.
                    RsaEncryptionLegacy.EncryptWithPemPublicKey(publicKeyPath, usernameBytes);
                    byte[] validationEnvelope = OvWorksLoginEnvelope.Wrap(passwordBytes);
                    try { RsaEncryptionLegacy.EncryptWithPemPublicKey(publicKeyPath, validationEnvelope); }
                    finally { Array.Clear(validationEnvelope, 0, validationEnvelope.Length); }
                    protectedUsername = new OvWorksProtectedValue(usernameBytes);
                    protectedPassword = new OvWorksProtectedValue(passwordBytes);
                    username = null;
                    password = null;
                    usernameTextBox.Clear();
                    passwordTextBox.Clear();

                    using (OvWorksClientDashboardForm ovWorksClientDashboardForm = new OvWorksClientDashboardForm(
                        serialNumberTextBox.Text, hostTextBox.Text, protectedUsername,
                        profileTextBox.Text, protectedPassword, ovWorksHash.hashStatus))
                    {
                        this.Hide();
                        try
                        {
                            ovWorksClientDashboardForm.ShowDialog();
                        }
                        finally
                        {
                            this.Show();
                            this.Activate();
                        }
                    }
                }
                finally
                {
                    username = null;
                    password = null;
                    usernameTextBox.Clear();
                    passwordTextBox.Clear();
                    if (protectedUsername != null) protectedUsername.Dispose();
                    if (protectedPassword != null) protectedPassword.Dispose();
                    if (usernameBytes != null) Array.Clear(usernameBytes, 0, usernameBytes.Length);
                    if (passwordBytes != null) Array.Clear(passwordBytes, 0, passwordBytes.Length);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "보안 파일 다운로드 또는 로그인 준비에 실패했습니다.\r\n\r\n" + ex.Message,
                    "로그인 초기화 오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
