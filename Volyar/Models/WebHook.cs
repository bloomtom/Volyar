using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Volyar.Models
{
    /// <summary>
    /// Encapsulates settings for a WebHook, and an simple implementation.
    /// </summary>
    public class WebHook
    {
        /// <summary>
        /// The endpoint to call.
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// The HTTP method to use: Put or post.
        /// </summary>
        public string Method { get; set; }
        /// <summary>
        /// A username if needed for http authentication.
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// A password if needed for http authentication.
        /// </summary>
        public string Password { get; set; }

        public WebHook()
        {
        }

        /// <summary>
        /// Calls this WebHook's Url endpoint with optional authentiation based on Username being not null.
        /// </summary>
        /// <param name="body">A string content body to post/put.</param>
        /// <returns></returns>
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
