using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace csharpOvWorksClient_1._0._0
{
    internal class OvWorksConfig
    {
        public string _config_path;
        public string _viewer_path;

        public string _config_fname;
        public string _vm_console_fname;
        public string _ca_cert_fname;
        public string _hashTarget_fname;
        public string _audit_fname;

        private Boolean virtviewer_installed;// = false;

        public OvWorksConfig() 
        {
            _config_path = "C:\\Users\\" + Environment.UserName + "\\AppData\\Local\\OVWorks\\OVWorksClientSetup\\conf";
            //C:\Program Files\VirtViewer v11.0-256\bin
            _viewer_path = "C:\\Program Files\\ovworksViewer v11.0-1\\bin";
            _hashTarget_fname = "C:\\Program Files (x86)\\OVWorks\\OVWorksClientSetup\\csharpOvWorksClient_1.0.0.exe\\";
            _audit_fname = "aa5f098ea51d78a06db4ca46cbc6c5c87ad92ef0f846ea6c5888b0e5c1d59c5e.e9b";
            //_viewer_path = ViewerAppCheck();
            ConfigDirectoryCreate();
        }

        public void ConfigDirectoryCreate()
        {
            if (!Directory.Exists(_config_path))
                Directory.CreateDirectory(_config_path);
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

    }
}
