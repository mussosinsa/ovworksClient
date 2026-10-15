using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace csharpOvWorksClient_1._0._0
{
    internal class OvWorksConfig
    {
        public string _config_path;
        public string _viewer_path;
        public string _hashTarget_Path;

        public string _config_fname;
        public string _vm_console_fname;
        public string _ca_cert_fname;

        //public string _hashTarget_fname;
        // -2026-10-01-
        // 무결성 검사를 위한 해시 파일(SHA512)
        public string _hashFileName;
                
        // 무결성 검사를 위한 대상 파일
        public string _hashTargetFileName;

        public string _hashResultTemp_fname;
        public string _audit_fname;
        public string _SSLCertificate_fname;
        public string _publicKey_fname;

        public string[] _hashTargetFiles;

        private Guid _UUID;

        private Boolean virtviewer_installed;// = false;

        public OvWorksConfig()
        {
            // -2026-10-01-
            // 사용자별로 파일을 저장하기 위한 UUID(고정값)
            _UUID = Guid.Parse("dceffc58-c55c-4358-87ca-825ca465d442");
            Guid deterministicGuid = GuidExtensions.CreateVersion5WithSha512(_UUID, "S09A27E45O63L81L");
            //_hashFileName = deterministicGuid.ToString() + ".fot";
            deterministicGuid = GuidExtensions.CreateVersion5WithSha512(_UUID, "AUDIT");
            _audit_fname = deterministicGuid.ToString() + ".e9b";
            deterministicGuid = GuidExtensions.CreateVersion5WithSha512(_UUID, "HASH");
            _hashResultTemp_fname = deterministicGuid.ToString() + ".hrt";
            deterministicGuid = GuidExtensions.CreateVersion5WithSha512(_UUID, "CERTIFICATE");
            _SSLCertificate_fname = deterministicGuid.ToString() + ".crt";
            deterministicGuid = GuidExtensions.CreateVersion5WithSha512(_UUID, "PUBLICKEY");
            _publicKey_fname = deterministicGuid.ToString() + ".pem";

            _hashTargetFileName = "csharpOvWorksClient_1.0.0.exe";
            _hashTargetFiles = new[] { "BouncyCastle.Crypto.dll", "csharpOvWorksClient_1.0.0.exe" , "Newtonsoft.Json.dll" };

            _config_path = "C:\\Users\\" + Environment.UserName + "\\AppData\\Local\\Saeoll\\OV-WorksClient4.5.2\\conf";
            //C:\Program Files\VirtViewer v11.0-256\bin
            //C:\Program Files\ovworksViewer v11.0-1\bin
            //_viewer_path = "C:\\Program Files\\VirtViewer v11.0-256\\bin";
            _viewer_path = "C:\\Program Files\\ovworksViewer v11.0-1\\bin";
            //_hashTarget_fname = "C:\\Program Files (x86)\\Saeoll\\OV-Works Client 4.5.1\\csharpOvWorksClient_1.0.0.exe\\";
            //_hashTarget_Path = "C:\\Program Files (x86)\\Saeoll\\OV-Works Client 4.5.2";
            _hashTarget_Path = "C:\\Users\\" + Environment.UserName + "\\AppData\\Local\\Saeoll\\OV-WorksClient4.5.2\\hash";

            //_audit_fname =          "aa5f098ea51d78a6db.e9b";
            //_hashResultTemp_fname = "a5f098ea51d78a06db.tmp";
            //_SSLCertificate_fname = "c87ad92ef0f846ea87.crt";
            //_publicKey_fname = "e5c1d59c5ee9b5ad9e.pem";

            //_viewer_path = ViewerAppCheck();
            ConfigDirectoryCreate();
        }

        public void ConfigDirectoryCreate()
        {
            if (!Directory.Exists(_config_path))
                Directory.CreateDirectory(_config_path);

            if (!Directory.Exists(_config_path))
                Directory.CreateDirectory(_hashTarget_Path);

            if (!Directory.Exists(_viewer_path))
                Directory.CreateDirectory(_viewer_path);

        }
        public void FileConfigFlush(string fname)
        {
            if (File.Exists(fname))
            {
                File.Delete(fname);
                //MessageBox.Show("DELETE OK");
            }
        }
        public string FindFile(string directory, string fileName)
        {
            string foundFileName = null;
            try
            {
                foundFileName = Directory.GetFiles(directory, fileName).FirstOrDefault();
                if (String.IsNullOrEmpty(foundFileName))
                {
                    foreach (string dir in Directory.GetDirectories(directory))
                    {
                        foundFileName = FindFile(dir, fileName);
                        if (!String.IsNullOrEmpty(foundFileName))
                            break;
                    }
                }
            }
            catch { } // The most likely exception is UnauthorizedAccessException
                      // and there is not much to do about that
            return foundFileName;
        }
        public Boolean FindRegistry()
        {
            RegistryKey pregkey;
            pregkey = Registry.CurrentUser;
            pregkey = pregkey.OpenSubKey(@"Software");
            String[] _software = pregkey.GetSubKeyNames();
            string virtviewer_name = "VIRTVIEWER";
            foreach (String s in _software)
                if (s.ToUpper().Equals(virtviewer_name))
                {
                    virtviewer_installed = true;
                    //break;                    
                    
                    return true;
                }
            return false;            
        }
        
        public string ViewerAppCheck()
        {
            virtviewer_installed = FindRegistry();
            if (virtviewer_installed)
            {
                return FindFile(@"C:\Program Files", "remote-viewer.exe");
            }
            else
            {
                MessageBox.Show("Remote Viewer가 설치되지 않았습니다. \n설치해야 콘솔(Console) 작업을 수행할 수 있습니다.", "Remote Viewer 에러",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return String.Empty;
        }        
        public string userFileName(string username)
        {
            Guid deterministicGuid = GuidExtensions.CreateVersion5WithSha512(_UUID, username);            
            return deterministicGuid.ToString();
        }
    }
}
