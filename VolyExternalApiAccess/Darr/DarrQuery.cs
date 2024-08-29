using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Net;
using System.Collections;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Json;

namespace VolyExternalApiAccess.Darr
{
    public enum DarrApiVersion
    {
        None,
        V3
    }

    internal class DarrApiVersionResponse
    {
        [JsonProperty("current")]
        public string Current { get; set; }

        [JsonProperty("deprecated")]
        public object[] Deprecated { get; set; }
    }

    public abstract class DarrQuery
    {
        public readonly string baseUrl;
        protected readonly string baseApi;
        protected readonly string apiKey;
        protected readonly string username;
        protected readonly string password;

        public DarrQuery(string baseUrl, string apiKey, DarrApiVersion darrApiVersion, string username = null, string password = null)
        {
            if (baseUrl.EndsWith("/")) { baseUrl = baseUrl[..^1]; }
            this.baseUrl = baseUrl;
            this.apiKey = System.Web.HttpUtility.UrlEncode(apiKey);
            this.username = username;
            this.password = password;

            switch (darrApiVersion)
            {
                case DarrApiVersion.V3:
                    baseApi = "/api/v3/";
                    break;
                case DarrApiVersion.None:
                    baseApi = "/api/";
                    break;
                default:
                    throw new Exception("API version {} not supported");
            }
        }

        protected async Task<ApiResponse<T>> QueryApiAsync<T>(Func<HttpClient, Task<ApiResponse<T>>> apiQuery)
        {
            return await QueryApiAsync(apiQuery, username, password);
        }

        protected static async Task<ApiResponse<T>> QueryApiAsync<T>(Func<HttpClient, Task<ApiResponse<T>>> apiQuery, string username, string password)
        {
            using HttpClient client = new();
            client.Timeout = TimeSpan.FromSeconds(20);
            if (username != null && password != null)
            {
                var credentialsBytes = Encoding.ASCII.GetBytes($"{username}:{password}");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentialsBytes));
            }
            return await apiQuery(client);
        }

        public static async Task<DarrApiVersion> QueryApiVersionAsync(string baseUrl, string username, string password)
        {
            var result = await QueryApiAsync<DarrApiVersionResponse>(async (client) =>
            {
                string requestUri = $"{baseUrl}/api";
                var httpResponse = await client.GetAsync(requestUri);
                if (!httpResponse.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to query Darr API version. Url: {requestUri} Status: {httpResponse.StatusCode}");
                }
                return new ApiResponse<DarrApiVersionResponse>(await httpResponse.Content.ReadFromJsonAsync<DarrApiVersionResponse>(), httpResponse.StatusCode);
            }, username, password);

            return result.Value.Current switch
            {
                "v3" => DarrApiVersion.V3,
                _ => DarrApiVersion.None,
            };
        }
    }
}
