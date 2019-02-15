using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;

namespace VolyExternalApiAccess.Darr.Sonarr
{
    public class SonarrQuery : DarrQuery<SonarrParsed>
    {
        public SonarrQuery(string baseUrl, string apiKey, string username = null, string password = null) : base(baseUrl, apiKey, username, password)
        {
        }

        public SonarrParsed Find(string fullPath)
        {
            return QueryApi((client) =>
            {
                HttpResponseMessage result;
                try
                {
                    result = client.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/api/parse?apikey={apiKey}&path={System.Web.HttpUtility.UrlEncode(fullPath)}")).Result;
                    if (!result.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"Status {result.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to query Sonarr API with URL {baseUrl}.", ex);
                }

                SonarrParsed deserialized;
                try
                {
                    var content = result.Content.ReadAsStringAsync().Result;
                    deserialized = SonarrParsed.FromJson(content);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to deserialize Sonarr API result with URL {baseUrl}.", ex);
                }

                return deserialized;
            });
        }
    }
}
