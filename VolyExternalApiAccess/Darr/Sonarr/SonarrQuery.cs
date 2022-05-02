using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Net;

namespace VolyExternalApiAccess.Darr.Sonarr
{
    public class SonarrQuery : DarrQuery
    {
        public SonarrQuery(string baseUrl, string apiKey, string username = null, string password = null) : base(baseUrl, apiKey, username, password)
        {
        }

        public ApiResponse<SonarrParsed> Find(string fullPath)
        {
            return QueryApi((client) =>
            {
                HttpResponseMessage result;
                string responseContent;
                string query = $"{baseUrl}/api/parse";
                try
                {
                    result = client.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"{query}?apikey={apiKey}&path={System.Web.HttpUtility.UrlEncode(fullPath)}")).Result;
                    responseContent = result.Content.ReadAsStringAsync().Result;
                }
                catch (Exception ex)
                {
                    return new ApiResponse<SonarrParsed>(null, HttpStatusCode.BadRequest, $"Failed to query Sonarr API with URL {query}. Exception: {ex}");
                }

                if (!result.IsSuccessStatusCode)
                {
                    return new ApiResponse<SonarrParsed>(null, result.StatusCode, $"Failed to query Radarr API with URL {query}.");
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
