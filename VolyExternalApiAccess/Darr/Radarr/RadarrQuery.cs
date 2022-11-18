using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VolyExternalApiAccess.Darr.Radarr
{
    internal class RadarrCached
    {
        public ICollection<Movie> Movies { get; set; } = null;
        public Stopwatch LastFilled { get; set; } = new Stopwatch();
    }

    public enum RadarrApiVersion
    {
        None,
        V3
    }

    public class RadarrQuery : DarrQuery
    {
        private readonly TimeSpan cacheTimeout;
        private static readonly ConcurrentDictionary<string, RadarrCached> _cached = new ConcurrentDictionary<string, RadarrCached>();
        private readonly string baseApi;

        public RadarrQuery(string baseUrl, string apiKey, string username = null, string password = null, TimeSpan? cacheTimeout = null, RadarrApiVersion apiVersion = RadarrApiVersion.V3)
            : base(baseUrl, apiKey, username, password)
        {
            this.cacheTimeout = cacheTimeout ?? TimeSpan.FromMinutes(30);
            _cached.GetOrAdd(base.baseUrl, new RadarrCached());

            switch (apiVersion)
            {
                case RadarrApiVersion.V3:
                    baseApi = "/api/v3/";
                    break;
                default:
                    baseApi = "/api/";
                    break;
            }
        }

        public ApiResponse<string> Version()
        {
            return QueryApi<string>((client) =>
            {
                var result = client.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}{baseApi}?apikey={apiKey}")).Result;
                string version = (string)JObject.Parse(result.Content.ReadAsStringAsync().Result)["version"];
                return new ApiResponse<string>(version, result.StatusCode);
            });
        }

        protected virtual ApiResponse<ICollection<Movie>> GetMovies()
        {
            return QueryApi((client) =>
            {
                string query = $"{baseUrl}{baseApi}movie";
                HttpResponseMessage result;
                string message = string.Empty;
                try
                {
                    result = client.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"{query}?apikey={apiKey}")).Result;
                }
                catch (Exception ex)
                {
                    return new ApiResponse<ICollection<Movie>>(null, HttpStatusCode.BadRequest, $"Failed to query Radarr API with URL {query}. Exception: {ex}");
                }

                if (!result.IsSuccessStatusCode)
                {
                    return new ApiResponse<ICollection<Movie>>(null, result.StatusCode, $"Failed to query Radarr API with URL {query}.");
                }

                List<Movie> deserialized;
                try
                {
                    var content = result.Content.ReadAsStringAsync().Result;
                    deserialized = Movie.FromJson(content);
                }
                catch (Exception ex)
                {
                    return new ApiResponse<ICollection<Movie>>(null, HttpStatusCode.BadRequest, $"Failed to deserialize Radarr API result with URL {query} Exception: {ex})");
                }

                return new ApiResponse<ICollection<Movie>>(deserialized, result.StatusCode);
            });
        }

        private ApiResponse<ICollection<Movie>> GetCached(TimeSpan? invalidation = null)
        {
            invalidation = invalidation ?? cacheTimeout;

            var cachedItem = _cached[baseUrl];
            lock (cachedItem)
            {
                if (cachedItem.Movies == null || cachedItem.LastFilled.Elapsed > invalidation)
                {
                    var response = GetMovies();
                    if (response.IsSuccessStatusCode)
                    {
                        cachedItem.Movies = response.Value;
                        cachedItem.LastFilled.Restart();
                    }
                    return response;
                }
                return new ApiResponse<ICollection<Movie>>(cachedItem.Movies, HttpStatusCode.OK);
            }
        }

        /// <summary>
        /// Gets a collection of movies from the cache, or queries the cache
        /// </summary>
        /// <returns></returns>
        public ApiResponse<ICollection<Movie>> Movies()
        {
            return GetCached();
        }

        public ApiResponse<IEnumerable<Movie>> Where(Func<Movie, bool> condition)
        {
            var response = GetCached();
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return new ApiResponse<IEnumerable<Movie>>(Enumerable.Empty<Movie>(), response.StatusCode, response.ErrorDetails);
            }

            var filtered = response.Value?.Where(condition);
            if (filtered == null || !filtered.Any())
            {
                // Try again if Radarr hasn't been queried very recently.
                response = GetCached(TimeSpan.FromSeconds(2));
                filtered = response.Value.Where(condition);
            }
            return new ApiResponse<IEnumerable<Movie>>(filtered, response.StatusCode, response.ErrorDetails);
        }
    }
}