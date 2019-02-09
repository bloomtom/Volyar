using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace VolyConverter
{
    public class WebHook
    {
        public string Url { get; set; }
        public string Method { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public WebHook()
        {
        }

        public async Task<HttpResponseMessage> CallAsync(string body)
        {
            NetworkCredential credentials = new NetworkCredential(Username, Password);
            var handler = new HttpClientHandler { Credentials = Username == null ? null : credentials };
            HttpClient client = new HttpClient(handler);
            switch (Method.ToUpperInvariant())
            {
                case "PUT":
                    return await client.PutAsync(Url, new StringContent(body));
                case "POST":
                    return await client.PostAsync(Url, new StringContent(body));
                default:
                    return null;
            }
        }
    }
}
