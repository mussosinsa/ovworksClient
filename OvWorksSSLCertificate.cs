using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace csharpOvWorksClient_1._0._0
{
    internal class OvWorksSSLCertificate
    {
        public OvWorksURI ovWorksURI = new OvWorksURI();
        public string _URL = string.Empty;

        public OvWorksSSLCertificate(string hostprotocol, string host)
        {

            _URL = hostprotocol + "://" + host + ovWorksURI._api_uri + ovWorksURI._api_cert;
            //MessageBox.Show(_URL);
        }

        public async void SSLCertificateDownload()
        {
            using (var client = new HttpClient())
            {
                try
                {
                    using (var s = await client.GetStreamAsync(_URL))
                    {
                        using (var fs = new FileStream("ca.crt", FileMode.OpenOrCreate))
                        {
                            await s.CopyToAsync(fs);
                        }
                    }                    
                }
                catch (HttpRequestException exp)
                {
                    MessageBox.Show($"An error occurred: {exp.Message}");
                }
                catch (Exception exp)
                {
                    MessageBox.Show($"An error occurred: {exp.Message}");
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
