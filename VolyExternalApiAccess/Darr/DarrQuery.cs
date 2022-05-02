using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Net;

namespace VolyExternalApiAccess.Darr
{
    public abstract class DarrQuery
    {
        public readonly string baseUrl;
        protected readonly string apiKey;
        protected readonly string username;
        protected readonly string password;

        public DarrQuery(string baseUrl, string apiKey, string username = null, string password = null)
        {
            if (baseUrl.EndsWith("/")) { baseUrl = baseUrl.Substring(0, baseUrl.Length - 1); }
            this.baseUrl = baseUrl;
            this.apiKey = System.Web.HttpUtility.UrlEncode(apiKey);
            this.username = username;
            this.password = password;
        }

        protected ApiResponse<T> QueryApi<T>(Func<HttpClient, ApiResponse<T>> apiQuery)
        {
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(20);
                if (username != null && password != null)
                {
                    var credentialsBytes = Encoding.ASCII.GetBytes($"{username}:{password}");
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentialsBytes));
                }
                return apiQuery.Invoke(client);
            }
        }
    }
}
