using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace csharpOvWorksClient_1._0._0
{
    internal class OvWorksSecureString
    {
        public SecureString secureUsername;
        public SecureString securePassword;
        public string charPool;

        private Random random;
        public OvWorksSecureString() 
        {
            securePassword = new SecureString();
            charPool = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            random = new Random();
        }
        //private TextBox txtPassword;
        /*
        public LoginForm()
        {
            txtPassword = new TextBox
            {
                PasswordChar = '●',   // 화면에는 마스킹 문자만 표시
                Location = new System.Drawing.Point(20, 20),
                Width = 200
            };

            // 텍스트 자체를 바인딩하지 않고, 키 입력을 직접 가로챈다
            txtPassword.KeyPress += TxtPassword_KeyPress;

            this.Controls.Add(txtPassword);
        }
        */
        /*
        private void BtnLogin_Click(object sender, EventArgs e)
        {
            securePassword.MakeReadOnly();

            // 실제 인증 시점에만 잠깐 평문으로 변환해서 사용
            string plainPassword = SecureStringToPlain(securePassword);
            try
            {
                // Authenticate(txtUsername.Text, plainPassword);
            }
            finally
            {
                // 사용 후 최대한 빨리 GC 대상이 되도록
                plainPassword = null;
            }
        }
        */
        public char RandomChar()
        {
            // 미리 지정한 문자 풀에서 무작위로 하나 선택
            int randomIndex = random.Next(charPool.Length);
            char randomChar = charPool[randomIndex];
            return randomChar;
        }
        /*
        private void TxtPassword_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Back)
            {
                // 백스페이스 처리
                if (securePassword.Length > 0)
                    securePassword.RemoveAt(securePassword.Length - 1);
            }
            //else if (!char.IsControl(e.KeyChar))
            else if (charPool.Contains(e.KeyChar))
            {
                securePassword.AppendChar(e.KeyChar);
                e.Handled = true; // 입력한 키 무효화
                //passwordTextBox.Text += randomChar().ToString();

                // 텍스트박스 커서를 맨 끝으로 이동
                //passwordTextBox.SelectionStart = passwordTextBox.Text.Length;
            }

            // TextBox 자체의 텍스트 편집은 기본 동작에 맡기되(PasswordChar가 마스킹 처리),
            // 실제 값을 저장하는 소스는 securePassword로 이원화
        }
        */
        public string SecureStringToPlain(SecureString secure)
        {
            IntPtr ptr = System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocUnicode(secure);
            try
            {
                return System.Runtime.InteropServices.Marshal.PtrToStringUni(ptr);
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ZeroFreeGlobalAllocUnicode(ptr);
            }
        }
    }
}
