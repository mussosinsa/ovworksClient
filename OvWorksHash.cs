using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.X500;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace csharpOvWorksClient_1._0._0
{
    public class integritySW
    {
        private static bool _expired;
        public static bool Expired
        {
            get
            {
                // Reads are usually simple
                return _expired;
            }
            set
            {
                // You can add logic here for race conditions,
                // or other measurements
                _expired = value;
            }
        }
        // Perhaps extend this to have Read-Modify-Write static methods
        // for data integrity during concurrency? Situational.
    }
    internal class OvWorksHash
    {        
        public SHA512 Sha512 = SHA512.Create();
        public string hashFileName;
        public string targetFileName;        
        public byte[] hash512Value;
        public string hashStatus;

        public int hashCheckCnt;

        public OvWorksHash()
        {
            hash512Value = null;
            hashFileName = "11cdd5e7-d3c4-547f-801e-063ce80e18b1.fot";
            targetFileName = "csharpOvWorksClient_1.0.0.exe";
            hashStatus = "normal"; // normal(Success), warning, error(Failure), alert
            hashCheckCnt = 1;
        }
        // Compute the file's hash.
        public byte[] GetHashSha512(string filename)
        {
            using (FileStream stream = File.OpenRead(filename))
            {
                return Sha512.ComputeHash(stream);
            }
        }
        public static string CalculateSHA512(string filePath)
        {
            if (!File.Exists(filePath)) return string.Empty;
            using (var sha512 = SHA512.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hashBytes = sha512.ComputeHash(stream);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
            }
        }
        // Return a byte array as a sequence of hex values.
        public string BytesToString(byte[] bytes)
        {
            string result = "";
            foreach (byte b in bytes) result += b.ToString("x2");
            return result;
        }
        // Hash File Reader
        public byte[] hashFileReader(string filename)
        {
            //string _str;
            byte[] _byte = new byte[64];
            try
            {
                FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
                fileStream.Read(_byte, 0, 64);
                fileStream.Close();
            }
            catch (Exception e)
            {
                MessageBox.Show("File Read Exception...\n" + e.Message, "Error", MessageBoxButtons.OK);
            }
            return _byte;
        }

        //
        private string getPCUsername()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            string fullName = identity.Name; // 예: "MYDOMAIN\username"

            //Console.WriteLine($"전체 계정명: {fullName}");
            //Log(enLogLevel.Info_L1, $" Domain\\username : {fullName} ");

            // 백슬래시(\)를 기준으로 도메인과 사용자명 분리
            string[] parts = fullName.Split('\\');
            if (parts.Length == 2)
            {
                string domain = parts[0];
                string user = parts[1];
                //Console.WriteLine($"도메인: {domain}, 사용자: {user}");
                //Log(enLogLevel.Info_L1, $"도메인: {domain}, 사용자: {user}");
            }
            return parts[1];
        }

        // -2026-10-14-
        // File 읽기, 쓰기, 삭제, 모든 권한 사용 가능 설정
        private bool FileACLEnable(string filePath)
        {
            try
            {
                string pcUsername = getPCUsername();
                //MessageBox.Show(pcUsername);
                //FileInfo fileInfo = new(filePath);
                //FileInfo fileInfo = new FileInfo(pcUsername);

                // 1. 파일의 기존 보안 설정(Access Control List)
                FileInfo fileInfo = new FileInfo(filePath);
                FileSecurity fSecurity = fileInfo.GetAccessControl();

                // 2. 부여할 권한 정의 (Read, Write, Delete 권한 결합)
                FileSystemRights rights = FileSystemRights.Read | FileSystemRights.Write | FileSystemRights.Delete | FileSystemRights.Modify;

                //fSecurity.AddAccessRule(new FileSystemAccessRule(username, rights, AccessControlType.Allow));
                FileSystemAccessRule accessRule = new FileSystemAccessRule(pcUsername, rights, AccessControlType.Deny);
                // 3. 변경된 보안 설정을 파일에 최종 적용
                // 규칙 제거
                fSecurity.RemoveAccessRule(accessRule);
                // 3. 변경된 보안 설정을 파일에 최종 적용
                fileInfo.SetAccessControl(fSecurity);
                //Log(enLogLevel.Info_L1, $" Enable OK : {registryFile} ");
                return true;
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"오류 발생: {ex.Message}");
                return false;
            }
        }
        // -2026-10-14-
        // File 읽기, 쓰기, 삭제, 모든 권한 사용 불가능 설정
        private bool FileACLDisable(string filePath)
        {
            try
            {
                string pcUsername = getPCUsername();
                //FileInfo fileInfo = new(filePath);

                // 1. 파일의 기존 보안 설정(Access Control List)
                FileInfo fileInfo = new FileInfo(filePath);
                FileSecurity fSecurity = fileInfo.GetAccessControl();

                // 2. 부여할 권한 정의 (Read, Write, Delete 권한 결합)
                FileSystemRights rights = FileSystemRights.Read | FileSystemRights.Write | FileSystemRights.Delete | FileSystemRights.Modify;

                //fSecurity.AddAccessRule(new FileSystemAccessRule(username, rights, AccessControlType.Deny));
                FileSystemAccessRule accessRule = new FileSystemAccessRule(pcUsername, rights, AccessControlType.Deny);
                // 3. 변경된 보안 설정을 파일에 최종 적용
                // 규칙 제거
                fSecurity.AddAccessRule(accessRule);
                fileInfo.SetAccessControl(fSecurity);
                //Log(enLogLevel.Info_L1, $" Disable OK : {registryFile} ");
                return true;
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"오류 발생: {ex.Message}");
                return false;
            }
        }
        public static string ReadAllTextShared(string path)
        {
            // FileShare.ReadWrite를 지정하여 다른 프로세스가 쓰고 있는 중에도 읽을 수 있도록 허용합니다.
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
            {
                return streamReader.ReadToEnd();
            }
        }

        // 3. 기존 JSON 저장본을 불러와 현재 파일 상태와 비교 검증
        // 반환값 정의
        // flags = { true, true, true };
        // BouncyCastle.Crypto.dll, csharpOvWorksClient_1.0.0.exe, Newtonsoft.Json.dll
        public async Task<bool[]> VerifyIntegrity(string targetDirectory, string resultJsonPath)
        { 
            bool[] flags = { true, true, true };
            
            //bool fileACLEnabled = FileACLEnable(resultJsonPath);
            //MessageBox.Show(resultJsonPath + "\n");

            if (!File.Exists(resultJsonPath)) 
            {
                //Console.WriteLine("[오류] 무결성 검증용 JSON 파일이 존재하지 않습니다.");                
                MessageBox.Show("무결성 검증용 파일이 존재하지 않습니다.");
                flags[0] = false; flags[1] = false; flags[2] = false;
                return flags;            
            }
            //MessageBox.Show(resultJsonPath + "\n");
            // 기존 저장된 데이터 로드
            OvWorksConfig ovWorksConfig = new OvWorksConfig();

            //string jsonString = File.ReadAllText(resultJsonPath);
            string jsonString = ReadAllTextShared(resultJsonPath);

            //var savedRegistry = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);
            var savedRegistry = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
            //User user = JsonConvert.DeserializeObject<User>(jsonString);

            if (savedRegistry == null) return flags;
            bool isAllValid = true;
            int index = 0;
            // 기존 기록된 파일 목록을 순회하며 검사
            foreach (var kvp in savedRegistry)
            {
                string relativePath = kvp.Key;
                string expectedHash = kvp.Value;
                string absolutePath = Path.Combine(targetDirectory, relativePath);
                // 1. 파일 누락 여부 확인
                if (!File.Exists(relativePath))
                {
                    //Console.WriteLine($"[위해/경고] 파일 유실됨: {relativePath}");
                    //isAllValid = false;
                    flags[index++] = false;
                    continue;
                }
                // 2. 해시 일치 여부 확인 (변조 확인)                
                //string currentHash = CalculateSHA512(targetFiles[index++]);
                string currentHash = CalculateSHA512(ovWorksConfig._hashTargetFiles[index]);
                //currentHash = CalculateSHA256(targetFiles[index++]);
                //MessageBox.Show(currentHash + "\n\n" + expectedHash);
                if (currentHash != expectedHash)
                {
                    //Console.WriteLine($"[위해/경고] 파일 변조됨: {relativePath}");
                    //Console.WriteLine($"   - 예상 해시: {expectedHash}");
                    //Console.WriteLine($"   - 현재 해시: {currentHash}");                    
                    //isAllValid = false;
                    flags[index] = false;
                }
                else
                {
                    //Console.WriteLine($"[안전] 정상 파일: {relativePath}");                    
                }
                index++;
            }
            //MessageBox.Show(isAllValid.ToString());
            //return isAllValid;
            
            //bool fileACLDisabled = FileACLDisable(resultJsonPath);

            return flags;
        }
        public async void integrityCheck(string username_, string profile_)
        {
            OvWorksClientHandler ovWorksClientHandler = new OvWorksClientHandler();
            OvWorksConfig ovWorksConfig = new OvWorksConfig();
            
            hashCheckCnt++;
            // 현재 실행 디렉토리의 타켓 설정 : 배포시는 주석처리 필요
            //ovWorksConfig._hashTarget_Path = Environment.CurrentDirectory;
            bool[] result = await VerifyIntegrity(ovWorksConfig._hashTarget_Path,                
                Path.Combine(ovWorksConfig._hashTarget_Path, hashFileName));
            
            if (!result[0] || !result[1] || !result[2])
            {
                MessageBox.Show("무결성 검사 결과 변형된 부분이 발견되었습니다." +
                    "\r\n\n프로그램을 다시설치 해야 합니다. " +
                    "\r\n\n프로그램을 즉시 종료합니다.", "무결성 검사 결과", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                hashStatus = "error";

                // -2026-09-30-
                string job = "Startup Checked";
                string _xml;
                Random rnd = new Random();
                // Get local IP Address(IPv4) -2026-08-21-
                string hostName = Dns.GetHostName();
                string localIPv4Address = string.Empty;
                IPHostEntry ipEntry = Dns.GetHostEntry(hostName);
                foreach (IPAddress ip in ipEntry.AddressList)
                {
                    // Filter to get only IPv4 addresses
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        localIPv4Address = ip.ToString();
                        break;
                    } 
                }
                string hashtargets = hashStatus;
                int index = 0;
                foreach(bool rlt in result)
                {
                    if(rlt == false)
                    {
                        hashtargets += $" {ovWorksConfig._hashTargetFiles[index]}";
                    }
                    index++;
                }
                _xml = $"<event>" +
                $"<description>User {username_}@{profile_}-authz connected from '{localIPv4Address}' OV-Works Client 4.5.1 Integrity({hashCheckCnt.ToString()}) '{job}' {hashtargets} Resending data from {DateTime.Now.ToString("yyyy.MM.dd.tt hh:mm:ss")}</description>" +
                $"<severity>{hashStatus}</severity>" +
                $"<origin>OV-Works Client 4.5.1</origin>" +
                $"<custom_id>{DateTime.Now.ToString("Md") + rnd.Next(10, 100).ToString()}</custom_id>" +
                $"</event>";
                //-2026-10-01-
                ovWorksConfig._hashResultTemp_fname = ovWorksConfig.userFileName(username_) + ".hrt";
                bool ret = await ovWorksClientHandler.Txt2FileSave(_xml, ovWorksConfig._config_path, ovWorksConfig._hashResultTemp_fname);
                //Task.Delay(2000);
                //Application.Exit();
                //MessageBox.Show("test");
                Environment.Exit(1);
            }
            else
            {
                MessageBox.Show("무결성 검사 결과 변형된 부분이 없습니다.", "무결성 검사 결과", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                hashStatus = "normal";
            }
            // Read
            bool areWeAlive = integritySW.Expired;

            // Write
            // past deadline
            integritySW.Expired = true;
        }
        /*
        public async void integrityCheck(string username_, string profile_)
        {
            OvWorksClientHandler ovWorksClientHandler = new OvWorksClientHandler();
            OvWorksConfig ovWorksConfig = new OvWorksConfig();
            hashCheckCnt++;
            //usernameTextBox.Text = DateTime.Now.ToString();
            hash512Value = hashFileReader(hashFileName);
            byte[] targetHash512Value = GetHashSha512(targetFileName);
            //bool check= ovWorksHash.hash512Value.SequenceEqual(targetHash512Value);
            if (hash512Value.SequenceEqual(targetHash512Value) != true)
            {
                //MessageBox.Show($"해시:{ovWorksHash.BytesToString(ovWorksHash.hash512Value)}, {ovWorksHash.hash512Value.Length}\n" +
                //    $"타켓:{ovWorksHash.BytesToString(targetHash512Value)}, {targetHash512Value.Length}", "Failure", MessageBoxButtons.OK,MessageBoxIcon.Warning);
                MessageBox.Show("무결성 검사 결과 변형된 부분이 발견되었습니다." +
                    "\r\n\n프로그램을 재설치 해야 합니다. " +
                    "\r\n\n프로그램을 즉시 종료합니다.", "무결성 검사 결과", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                hashStatus = "error";

                // -2026-09-30-
                string job = "Startup Checked";
                string _xml;
                Random rnd = new Random();
                // Get local IP Address(IPv4) -2026-08-21-
                string hostName = Dns.GetHostName();
                string localIPv4Address = string.Empty;                
                IPHostEntry ipEntry = Dns.GetHostEntry(hostName);
                foreach (IPAddress ip in ipEntry.AddressList)
                {
                    // Filter to get only IPv4 addresses
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        localIPv4Address = ip.ToString();
                        break;
                    }
                }
                _xml = $"<event>" +
                $"<description>User {username_}@{profile_}-authz connected from '{localIPv4Address}' OV-Works Client 4.5.1 Integrity({hashCheckCnt.ToString()}) '{job}' {hashStatus} Resending data from {DateTime.Now.ToString("yyyy.MM.dd.tt hh:mm:ss")}</description>" +
                $"<severity>{hashStatus}</severity>" +
                $"<origin>OV-Works Client 4.5.1</origin>" +
                $"<custom_id>{DateTime.Now.ToString("Md") + rnd.Next(10, 100).ToString()}</custom_id>" +
                $"</event>";
                //-2026-10-01-
                ovWorksConfig._hashResultTemp_fname = ovWorksConfig.userFileName(username_) + ".hrt";
                bool ret = await ovWorksClientHandler.Txt2FileSave(_xml, ovWorksConfig._config_path, ovWorksConfig._hashResultTemp_fname);
                Application.Exit(); 
                //Environment.Exit(0);                
            }
            else
            {
                MessageBox.Show("무결성 검사 결과 변형된 부분이 없습니다.", "무결성 검사 결과", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                hashStatus = "normal";
            }
            // Read
            bool areWeAlive = integritySW.Expired;

            // Write
            // past deadline
            integritySW.Expired = true;
        }
        */
        public async Task unsavedAuditLogs(string username_, string profile_, 
            OvWorksConfig ovWorksConfig, OvWorksClientHandler ovWorksClientHandler)
        {
            string job = "Startup Checked";
            string _xml;
            Random rnd = new Random();
            // Get local IP Address(IPv4) -2026-08-21-
            string hostName = Dns.GetHostName();
            string localIPv4Address = string.Empty;
            IPHostEntry ipEntry = Dns.GetHostEntry(hostName);
            foreach (IPAddress ip in ipEntry.AddressList)
            {
                // Filter to get only IPv4 addresses
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIPv4Address = ip.ToString();
                    break;
                }
            }
            _xml = $"<event>" +
            $"<description>User {username_}@{profile_}-authz connected from '{localIPv4Address}' OV-Works Client 4.5.1 Integrity({hashCheckCnt.ToString()}) '{job}' {hashStatus} Resending data from {DateTime.Now.ToString("yyyy.MM.dd.tt hh:mm:ss")}</description>" +
            $"<severity>{hashStatus}</severity>" +
            $"<origin>OV-Works Client 4.5.1</origin>" +
            $"<custom_id>{DateTime.Now.ToString("Md") + rnd.Next(10, 100).ToString()}</custom_id>" +
            $"</event>";
            //-2026-10-01-
            ovWorksConfig._hashResultTemp_fname = ovWorksConfig.userFileName(username_) + ".hrt";
            bool ret = await ovWorksClientHandler.Txt2FileSave(_xml, ovWorksConfig._config_path, ovWorksConfig._hashResultTemp_fname);
        }
    }
}
