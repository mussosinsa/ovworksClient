using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Remoting.Contexts;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static csharpOvWorksClient_1._0._0.OvWorksURI;

namespace csharpOvWorksClient_1._0._0
{
    internal class OvWorksSSLCertificate
    {
        public OvWorksURI ovWorksURI = new OvWorksURI();
        public OvWorksConfig ovWorksConfig = new OvWorksConfig();
        public string _URL_CHK = string.Empty;
        public string _URL_SSL = string.Empty;
        public string _URL_publicKey = string.Empty;
        private string _crtfname = string.Empty;
        private string _publicKeyfname = string.Empty;

        public OvWorksSSLCertificate(string hostprotocol, string host)
        {

            _URL_CHK = hostprotocol + "://" + host + ovWorksURI._api_uri;
            _URL_SSL = hostprotocol + "://" + host + ovWorksURI._api_uri + ovWorksURI._api_cert;
            _URL_publicKey = hostprotocol + "://" + host + ovWorksURI._api_uri + ovWorksURI._api_publickey;
            _crtfname = ovWorksConfig._config_path + "\\" + ovWorksConfig._SSLCertificate_fname;
            _publicKeyfname = ovWorksConfig._config_path + "\\" + ovWorksConfig._publicKey_fname;
            //MessageBox.Show(_URL);
        }
        public async Task<bool> CheckApacheIpRestrictionAsync()
        {
            using (var client = new HttpClient())
            {
                try
                {
                    // 헤더 정보만 가져오는 GetAsync(또는 SendAsync)를 사용하면 본문 다운로드를 생략해 더 빠릅니다.
                    HttpResponseMessage response = await client.GetAsync(_URL_CHK);

                    if (response.IsSuccessStatusCode)
                    {
                        //Console.WriteLine("✅ [접근 성공] 현재 IP가 Apache 허용 목록에 포함되어 있습니다. (Status: 200 OK)");
                        //MessageBox.Show($" ({response.IsSuccessStatusCode})" + "프로그램 OK.\r\n");
                        return true;
                    }
                    else if (response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        //Console.WriteLine("❌ [접근 거부] Apache 'Require ip' 제한에 의해 차단되었습니다. (Status: 403 Forbidden)");
                        //Environment.Exit(0);
                        //MessageBox.Show($" ({HttpStatusCode.Forbidden})" + "프로그램을 즉시 종료합니다.\r\n");
                        //Application.Exit();
                        return false;
                    }
                    //else
                    //{
                    //Console.WriteLine($"⚠️ [기타 상태] 서버가 응답했으나 응답 코드가 다릅니다. (Status: {(int)response.StatusCode} {response.StatusCode})");
                    //}
                }
                catch (HttpRequestException e)
                {
                    //Console.WriteLine($"🌐 [네트워크 오류] 서버에 연결할 수 없습니다: {e.Message}");
                    //MessageBox.Show($"HttpRequestException.({HttpStatusCode.Forbidden})" + "프로그램을 즉시 종료합니다.\r\n");
                    //Application.Exit();
                    //MessageBox.Show($"SSL HttpRequestException.({e.Message})");
                    return false;
                }
                catch (TaskCanceledException)
                {
                    //Console.WriteLine("TaskCanceledException");
                    //MessageBox.Show($"TaskCanceledException.");
                    return false;
                }
                catch (Exception e)
                {
                    //Console.WriteLine($"Exception: {e.Message}");
                    //MessageBox.Show($"Exception.{e.Message}");
                    return false;
                }
                finally
                {
                    //return false;
                }
                return true;
            }
        }
        public async Task<bool> PublicKeyDownload(string _serialNum)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Add("X-Client-Serial", _serialNum);
                    using (var s = await client.GetStreamAsync(_URL_publicKey))
                    {
                        using (var fs = new FileStream(_publicKeyfname, FileMode.OpenOrCreate))
                        {
                            await s.CopyToAsync(fs);
                        }
                    }
                    return true;
                }
                //catch (HttpRequestException exp) when (HttpStatusCode.Forbidden == exp.StatusCode)
                //{
                // 404 Not Found 처리
                //}
                catch (HttpRequestException exp)
                {
                    //MessageBox.Show($"An HttpRequestException occurred: {exp.Message}");
                    return false;
                }
                catch (Exception exp)
                {
                    //MessageBox.Show($"An Exception occurred: {exp.Message}");
                    return false;
                }
                finally
                {
                    //TxtFileRead(@"ca.crt");
                    //MessageBox.Show("SSL Certificate Download Success.");
                }
            }            
        }
        public async Task<bool> SSLCertificateDownload()
        {
            using (var client = new HttpClient())
            {
                try
                {
                    using (var s = await client.GetStreamAsync(_URL_SSL))
                    {
                        using (var fs = new FileStream(_crtfname, FileMode.OpenOrCreate))
                        {
                            await s.CopyToAsync(fs);
                        }
                    }
                    return true;
                }
                //catch (HttpRequestException exp) when (HttpStatusCode.Forbidden == exp.StatusCode)
                //{
                    // 404 Not Found 처리
                //}
                catch (HttpRequestException exp)
                {
                    //MessageBox.Show($"An HttpRequestException occurred: {exp.Message}");
                    return false;
                }
                catch (Exception exp)
                {
                    //MessageBox.Show($"An Exception occurred: {exp.Message}");
                    return false;
                }
                finally
                {
                    //TxtFileRead(@"ca.crt");
                    //MessageBox.Show("SSL Certificate Download Success.");
                }
            }            
        }
    }
}
