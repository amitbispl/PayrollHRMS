using Newtonsoft.Json.Linq;
using System.Net;
using System.Text;

namespace HRMS.Service
{
    public class Worker : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
            var baseAddress = "https://api.zeptomail.in/v1.1/email";

            var http = (HttpWebRequest)WebRequest.Create(new Uri(baseAddress));
            http.Accept = "application/json";
            http.ContentType = "application/json";
            http.Method = "POST";
            http.PreAuthenticate = true;
            http.Headers.Add("Authorization", "<SEND_MAIL_TOKEN>");
            JObject parsedContent = JObject.Parse("{'from': { 'address': '<DOMAIN>'},'to': [{'email_address': {'address': 'aks.bispl@gmail.com','name': 'Amit saini'}}],'subject':'Test Email','htmlbody':'<div><b> Test email sent successfully.  </b></div>'}");
            Console.WriteLine(parsedContent.ToString());
            ASCIIEncoding encoding = new ASCIIEncoding();
            Byte[] bytes = encoding.GetBytes(parsedContent.ToString());

            Stream newStream = http.GetRequestStream();
            newStream.Write(bytes, 0, bytes.Length);
            newStream.Close();

            var response = http.GetResponse();

            var stream = response.GetResponseStream();
            var sr = new StreamReader(stream);
            var content = sr.ReadToEnd();
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
