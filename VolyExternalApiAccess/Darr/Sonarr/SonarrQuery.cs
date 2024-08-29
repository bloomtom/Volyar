using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Net;
using VolyExternalApiAccess.Darr.Radarr;
using System.Threading.Tasks;

namespace VolyExternalApiAccess.Darr.Sonarr
{
    public class SonarrQuery : DarrQuery
    {
        public SonarrQuery(string baseUrl, string apiKey, DarrApiVersion apiVersion, string username = null, string password = null) : base(baseUrl, apiKey, apiVersion, username, password)
        {
        }

        public async Task<ApiResponse<SonarrParsed>> FindAsync(string fullPath)
        {
            return await QueryApiAsync(async (client) =>
            {
                HttpResponseMessage result;
                string responseContent;
                string query = $"{baseUrl}{baseApi}parse";
                try
                {
                    result = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"{query}?apikey={apiKey}&path={System.Web.HttpUtility.UrlEncode(fullPath)}&title={System.Web.HttpUtility.UrlEncode(fullPath)}"));
                    responseContent = await result.Content.ReadAsStringAsync();
                }
                catch (Exception ex)
                {
                    return new ApiResponse<SonarrParsed>(null, HttpStatusCode.BadRequest, $"Failed to query Sonarr API with URL {query}. Exception: {ex}");
                }

                if (!result.IsSuccessStatusCode)
                {
                    return new ApiResponse<SonarrParsed>(null, result.StatusCode, $"Failed to query Sonarr API with URL {query}.");
                }

                SonarrParsed deserialized;
                try
                {
                    deserialized = SonarrParsed.FromJson(responseContent);
                }
                catch (Exception ex)
                {
                    return new ApiResponse<SonarrParsed>(null, result.StatusCode, $"Failed to deserialize Sonarr data with URL {query}.\nException: {ex}\nData: {responseContent}");
                }

                return new ApiResponse<SonarrParsed>(deserialized, result.StatusCode);
            });
        }
    }
}
