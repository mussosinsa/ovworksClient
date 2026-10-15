using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using static csharpOvWorksClient_1._0._0.OvWorksKeyboardHook;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace csharpOvWorksClient_1._0._0
{
    public partial class OvWorksLoginForm : Form
    {
        // SecureString -2026-09-20-
        //private SecureString secureUsername, securePassword;
        //private static string charPool;
        //private Random random;// = new Random();
        
        // -2026-10-02-
        // background timer
        //private static System.Timers.Timer monitorTimer2;

        // 차단할 원격제어 프로그램의 프로세스 이름 목록 (확장자 .exe 제외)
        private static readonly string[] BlockedProcesses = { "SnippingTool", "TeamViewer", "vncviewer", "tvnviewer", "AnyDesk", "mstsc", "TermService" };

        OvWorksSecureString securePasswordString;

        // Keyboard Hooking
        OvWorksKeyboardHook ovWorksKeyboardHook;

        // Data Integrity 
        OvWorksHash ovWorksHash;

        // Config
        OvWorksConfig ovWorksConfig;

        // Remote Access
        OvWorksRemoteAccess ovWorksRemoteAccess;

        // SSL Certificate Download
        OvWorksSSLCertificate ovWorksSSLCertificate;
        // - 2026-10-04 -
        // 이벤트 처리 없이 윈도우 메시지를 가로채어 이동시키는 방법도 있습니다.
        // 폼 코드에 아래 메서드를 오버라이드하면 타이틀바가 없어도 창 어디나 잡고 움직일 수 있습니다.
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        // 윈도우 스크린샷(Screenshot or printscreen) 방지 API
        [DllImport("user32.dll")]
        public static extern uint SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);
        public OvWorksLoginForm()
        {
            Task task = InitializeComponent();
            
            ovWorksConfig = new OvWorksConfig();

            ovWorksRemoteAccess = new OvWorksRemoteAccess();

            securePasswordString = new OvWorksSecureString();
            securePasswordString.secureUsername = new SecureString();
            securePasswordString.securePassword = new SecureString();
            //securePasswordString.charPool = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";            

            // Windows Screenshot protected
            // 화면캡처를 위해 일시 중지 -2026-08-26-
            SetWindowDisplayAffinity(this.Handle, 0x01); // WDA_MONITOR(0x01), WDA_NONE(0x00), WDA_EXCLUDEFROMCAPTURE(0x00)

            // 화면캡처를 위해 일시 중지 -2026-08-26-
            monitorTimer.Enabled = true;
            // monitorTimer.Enabled = false;

            //SetmonitorTimer2();

            // Keyboard Hooking
            // 화면캡처를 위해 일시 중지 -2026-08-26-
                        
            ovWorksKeyboardHook = new OvWorksKeyboardHook();
            ovWorksKeyboardHook.KeyDown += new OvWorksKeyboardHook.KeyboardHookCallback(keyboardHook_KeyDown);
            ovWorksKeyboardHook.KeyUp += new OvWorksKeyboardHook.KeyboardHookCallback(keyboardHook_KeyUp);
            ovWorksKeyboardHook.Install();
            
            // Data Integrity Check 
            ovWorksHash = new OvWorksHash();
            //hashCheck();
            //ovWorksHash.integrityCheck();            
            if (!integritySW.Expired)
            {
                ovWorksHash.integrityCheck(usernameTextBox.Text, profileTextBox.Text);
            }
            BlockPortInbound();

            // -2026-10-12-
            /*
            string keyPath = @"SYSTEM\CurrentControlSet\Control\Terminal Server";            
            int value = 1; // 0 = 허용, 1 = 차단
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(keyPath, writable: true))
            {
                if (key != null)
                {
                    key.SetValue("fDenyTSConnections", value, RegistryValueKind.DWord);
                }
            }
            */
            // Handle the ApplicationExit event to know when the application is exiting.
            Application.ApplicationExit += new EventHandler(this.OnApplicationExit);
        }
        // Keyboard Hooking========================================================================================================================
        private void keyboardHook_KeyUp(OvWorksKeyboardHook.VKeys key)
        {
            //Console.WriteLine("[" + DateTime.Now.ToLongTimeString() + "] KeyUp Event {" + key.ToString() + "}");
        }
        private void keyboardHook_KeyDown(OvWorksKeyboardHook.VKeys key)
        {
            //Console.WriteLine("[" + DateTime.Now.ToLongTimeString() + "] KeyDown Event {" + key.ToString() + "}");
        }

        private VKeys KeyEncrypt()
        {
            Random rAnD = new Random(DateTime.Now.Millisecond);
            VKeys _keyencrypt = (VKeys)Enum.GetNames(typeof(VKeys)).GetValue(rAnD.Next(44, 77));
            return _keyencrypt;
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            // 화면캡처를 위해 일시 중지 -2026-08-26-
            
            ovWorksKeyboardHook.KeyDown -= new OvWorksKeyboardHook.KeyboardHookCallback(keyboardHook_KeyDown);
            ovWorksKeyboardHook.KeyUp -= new OvWorksKeyboardHook.KeyboardHookCallback(keyboardHook_KeyUp);
            ovWorksKeyboardHook.Uninstall();
            
            //monitorTimer2.Stop();
            //monitorTimer2.Dispose();
            //MessageBox.Show("Application.Exit()");
            //BlockPortRemove();
        }
        private async void BlockPortInbound()
        {
            string ruleName = string.Empty;
            int port = 0;
            foreach(string rulename in ovWorksRemoteAccess.ruleNames)
            {
                ruleName = rulename + ovWorksRemoteAccess.portToBlocks[port];
                if (await ovWorksRemoteAccess.CheckInboundRuleByNetsh(ruleName))
                {
                    port++;
                    continue;
                }
                else
                {
                    await ovWorksRemoteAccess.BlockPort(ruleName, ovWorksRemoteAccess.portToBlocks[port++], "in");
                }
            }
        }
        private async void BlockPortRemove()
        {
            string ruleName = string.Empty;
            int port = 0;
            foreach (string rulename in ovWorksRemoteAccess.ruleNames)
            {
                ruleName = rulename + ovWorksRemoteAccess.portToBlocks[port];
                if (!await ovWorksRemoteAccess.CheckInboundRuleByNetsh(ruleName))
                {
                    port++;
                    continue;
                }
                else
                {
                    await ovWorksRemoteAccess.DeletePortRule(ruleName);
                    port++;
                }
            }
        }
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
                MessageBox.Show("호스트(Host)가 입력되지 않았습니다. \n정확하게 입력 바랍니다.", "호스트(Host) 입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            BlockPortRemove();
            Application.Exit();
        }

        private void eyePictureBox_Click(object sender, EventArgs e)
        {
            //passwordTextBox.UseSystemPasswordChar = false;
        }

        private void eyePictureBox_MouseLeave(object sender, EventArgs e)
        {
            //passwordTextBox.UseSystemPasswordChar = true; 
        }
        /*
        public char RandomChar()
        {
            // 미리 지정한 문자 풀에서 무작위로 하나 선택
            int randomIndex = random.Next(charPool.Length);
            char randomChar = charPool[randomIndex];
            return randomChar;
        }
        */
        private async void loginButton_Click(object sender, EventArgs e)
        {
            // Encryption -2026-09-02-
            try
            {
                //string publicKeyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"public_key.pem");                
                /*
                if (!File.Exists(publicKeyPath))
                {
                    MessageBox.Show("공개키 파일이 없습니다.\r\n\r\n" + publicKeyPath);

                    return;
                }
                */
                string publicKeyPath = ovWorksConfig._config_path + "\\" + ovWorksConfig._publicKey_fname;
                string username = usernameTextBox.Text;
                string password = passwordTextBox.Text;

                if (!EmptyCheck())
                {
                    // SSL Ceritificate Download. - 2026-08-21 - 
                    ovWorksSSLCertificate = new OvWorksSSLCertificate("http", hostTextBox.Text);

                    // 단말기 IP 등록 확인 -2026-09-21-
                    bool caipra = await ovWorksSSLCertificate.CheckApacheIpRestrictionAsync();
                    if (!caipra)
                    {
                        MessageBox.Show($"[접근 거부] 연결이 차단 되었습니다.\r\n", "경고 메시지", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        //Application.Exit();                        
                    }
                    else
                    {
                        // public key Download -2026-09-23-
                        bool publickey = await ovWorksSSLCertificate.PublicKeyDownload(serialNumberTextBox.Text);
                        if (!File.Exists(publicKeyPath))
                        {
                            // -2026-10-01-
                            // 공개키 파일이 없는 경우 처리
                            MessageBox.Show("시리얼코드가 잘못되었습니다.\r\n");

                            return;
                        }
                        // SSL Ceritificate Download. - 2026-08-21 -
                        bool cert = await ovWorksSSLCertificate.SSLCertificateDownload();

                        // 여기서 암호화하는 것은 공개키가 쓸 수 있는 상태인지 지금 확인해 두기 위해서다.
                        // 실제로 보낼 자격증명은 대시보드가 인증할 때마다 새로 만든다. 한 번 만든
                        // 암호문을 들고 다니면 그것을 가로챈 쪽이 나중에 그대로 다시 보낼 수 있고,
                        // 그것이 이 화면이 평문을 넘기게 된 이유다.
                        string encryptedUsername = RsaEncryptionLegacy.EncryptWithPemPublicKey(publicKeyPath, username);

                        // Password 암호화
                        string encryptedPassword = RsaEncryptionLegacy.EncryptWithPemPublicKey(publicKeyPath, password);

                        // RSA 암호화된 ID:PW
                        string basicPayload = encryptedUsername + ":" + encryptedPassword;

                        // Basic 인증용 Base64
                        string authorization = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(basicPayload));

                        using (OvWorksClientDashboardForm ovWorksClientDashboardForm = new OvWorksClientDashboardForm(serialNumberTextBox.Text, hostTextBox.Text, usernameTextBox.Text, username, profileTextBox.Text, securePasswordString.securePassword, ovWorksHash.hashStatus, publicKeyPath))
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
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "암호화 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // 사용 후 최대한 빨리 GC 대상이 되도록                
                //plainPassword = null;
                passwordTextBox.Clear();
                securePasswordString.securePassword.Clear();
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
        /*
        private void killProcess()
        {
            foreach (Process pro in Process.GetProcesses())
            {
                if (pro.ProcessName.StartsWith("SnippingTool"))
                {
                    pro.Kill();
                }
            }
        }
        */
        private async Task<bool> killProcess(string processName)
        {
            try
            {
                foreach (Process pro in Process.GetProcesses())
                {
                    if (pro.ProcessName.StartsWith(processName))
                    {
                        pro.Kill();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"프로세스 모니터링 예외 발생 : {ex.Message}");
                return false;
            }
        }
        /// <summary>
        /// 백그라운드에서 실행되며 원격제어 프로세스를 주기적으로 감시하는 메서드
        /// </summary>
        private async void MonitorProcesses()
        {
            if(await IsRemoteDesktopSession())
            {
                MessageBox.Show($"사용할 수 없는 프로그램입니다.\r\n", "경고 메시지", MessageBoxButtons.OK, MessageBoxIcon.Stop, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
            }
            // Background process             
            var process = Process.GetProcesses();
            foreach (var x in process)
            {
                foreach (string processName in BlockedProcesses)
                {
                    var window = x.MainWindowHandle.ToInt32();
                    if (window == 0 && x.ProcessName.ToString().Equals(processName))
                    {
                        bool ret = await killProcess(processName);
                        //MessageBox.Show(new Form { TopMost = true }, $"사용할 수 없는 프로그램입니다.\r\n", "경고 메시지", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        MessageBox.Show($"사용할 수 없는 프로그램입니다.\r\n", "경고 메시지", MessageBoxButtons.OK, MessageBoxIcon.Stop, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                    }
                    else
                    {
                        
                    }
                    if (window == 0 && x.ProcessName.ToString().Equals("ĸó     "))
                    {
                        
                    }
                }
            }
        }
        private async Task<bool> IsRemoteDesktopSession()
        {
            return System.Windows.Forms.SystemInformation.TerminalServerSession;
        }

        private async void monitorTimer_Tick(object sender, EventArgs e)
        {
            //await IsRemoteDesktopSession();
            await Task.Run(() =>
            {
                MonitorProcesses();
            });
            /*
            Process[] mprocess;// = Process.GetProcessesByName("SnippingTool");
            foreach (string processName in BlockedProcesses)
            {
                mprocess = Process.GetProcessesByName(processName);
                if (mprocess.Length > 0)
                {
                    //textBox4.Text = "Running...";
                    //snippingtoolkillButton_Click(sender, new EventArgs());
                    //killProcess();
                    bool ret = await killProcess(processName);
                }
                else
                {
                    //textBox4.Text = "Nop";
                }
            }
            // Background process 
            var process = Process.GetProcesses();
            foreach (var x in process)
            {
                var window = x.MainWindowHandle.ToInt32();
                if (window == 0 && x.ProcessName.ToString().Equals("SnippingTool"))
                {
                    //textBox4.Text = "Background Running...";
                    //snippingtoolkillButton_Click(sender, new EventArgs());
                    killProcess();
                }
                else
                {
                    //textBox4.Text = "Background Nop";
                }
                if (window == 0 && x.ProcessName.ToString().Equals("ĸó     "))
                {
                    //textBox4.Text = "Background Running...      ...";
                }
            }
            */
        }
        /*
        private async void MonitorProcesses2()
        {
            // Background process            
            var process = Process.GetProcesses();
            foreach (var x in process)
            {
                foreach (string processName in BlockedProcesses)
                {
                    var window = x.MainWindowHandle.ToInt32();
                    if (window == 0 && x.ProcessName.ToString().Equals(processName))
                    {
                        bool ret = await killProcess(processName);
                        MessageBox.Show($"사용할 수 없는 프로그램입니다.\r\n", "경고 메시지", MessageBoxButtons.OK, MessageBoxIcon.Stop);                        
                    }
                    
                    else
                    {
                        
                    }
                    if (window == 0 && x.ProcessName.ToString().Equals("ĸó     "))
                    {
                        //textBox4.Text = "Background Running...      ...";
                    }
                    
                }
            }            
        }
        */
        /*
        private async void SetmonitorTimer2()
        {
            // Create a timer with a two second interval.
            monitorTimer2 = new System.Timers.Timer(100);
            // Hook up the Elapsed event for the timer. 
            monitorTimer2.Elapsed += monitorTimer2_OnTimedEvent;
            monitorTimer2.AutoReset = true;
            //monitorTimer2.Enabled = true;
            monitorTimer2.Enabled = false;
        }
        */
        /*
        private async void monitorTimer2_OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            //Console.WriteLine("The Elapsed event was raised at {0:HH:mm:ss.fff}", e.SignalTime);
            MonitorProcesses2();
        }
        */
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

        private void passwordTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '|' || e.KeyChar == ';' || e.KeyChar == ':' || e.KeyChar == '`')
            {
                e.Handled = true; // 입력한 키 무효화
                MessageBox.Show("사용 불가능한 특수문자가 포함되어 있습니다.", "경고 메시지", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (e.KeyChar == (char)Keys.Back)
            {
                // 백스페이스 처리
                if (securePasswordString.securePassword.Length > 0)
                    securePasswordString.securePassword.RemoveAt(securePasswordString.securePassword.Length - 1);
            }
            //else if (!char.IsControl(e.KeyChar))
            else if (securePasswordString.charPool.Contains(e.KeyChar))
            {
                if (securePasswordString.securePassword.Length < 16)
                {
                    securePasswordString.securePassword.AppendChar(e.KeyChar);
                    e.Handled = true; // 입력한 키 무효화
                    passwordTextBox.Text += securePasswordString.RandomChar().ToString(); // 랜덤키로 표시

                    // 텍스트박스 커서를 맨 끝으로 이동
                    passwordTextBox.SelectionStart = passwordTextBox.Text.Length;
                }
            }
            // TextBox 자체의 텍스트 편집은 기본 동작에 맡기되(PasswordChar가 마스킹 처리),
            // 실제 값을 저장하는 소스는 securePassword로 이원화
        } 

        private void usernameTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '|' || e.KeyChar == ';' || e.KeyChar == ':' || e.KeyChar == '`' || e.KeyChar == ' ')
            {
                e.Handled = true; // 입력한 키 무효화
                MessageBox.Show("사용 불가능한 특수문자가 포함되어 있습니다.", "경고 메시지", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }            
        }
        /*
        private void OvWorksLoginForm_MouseDown(object sender, MouseEventArgs e)
        {

        }
        */
        private void panel2_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }
    }
}
