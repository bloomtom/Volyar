using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Net.Http;

namespace VolyExternalApiAccess.Darr.Radarr
{
    internal class RadarrCached
    {
        public ICollection<Movie> Movies { get; set; } = null;
        public Stopwatch LastFilled { get; set; } = new Stopwatch();
    }

    public class RadarrQuery : DarrQuery<ICollection<Movie>>
    {
        private readonly TimeSpan cacheTimeout;
        private static ConcurrentDictionary<string, RadarrCached> cached = new ConcurrentDictionary<string, RadarrCached>();

        public RadarrQuery(string baseUrl, string apiKey, string username = null, string password = null, TimeSpan? cacheTimeout = null)
            : base(baseUrl, apiKey, username, password)
        {
            this.cacheTimeout = cacheTimeout ?? TimeSpan.FromMinutes(30);
            cached.GetOrAdd(baseUrl, new RadarrCached());
        }

        protected virtual ICollection<Movie> Get()
        {
            return QueryApi((client) =>
            {
                HttpResponseMessage result;
                try
                {
                    result = client.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/api/movie?apikey={apiKey}")).Result;
                    if (!result.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"Status {result.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to query Radarr API with URL {baseUrl}.", ex);
                }

                List<Movie> deserialized;
                try
                {
                    var content = result.Content.ReadAsStringAsync().Result;
                    deserialized = Movie.FromJson(content);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to deserialize Radarr API result with URL {baseUrl}.", ex);
                }

                return deserialized;
            });
        }

        private ICollection<Movie> GetCached(TimeSpan? invalidation = null)
        {
            invalidation = invalidation ?? cacheTimeout;

            var cachedItem = cached[baseUrl];
            lock (cachedItem)
            {
                if (cachedItem.Movies == null || cachedItem.LastFilled.Elapsed > invalidation)
                {
                    cachedItem.Movies = Get();
                    cachedItem.LastFilled.Restart();
                }
                return cachedItem.Movies;
            }
        }

        /// <summary>
        /// Gets a collection of movies from the cache, or queries the cache
        /// </summary>
        /// <returns></returns>
        public ICollection<Movie> Movies()
        {
            return GetCached();
        }

        public IEnumerable<Movie> Where(Func<Movie, bool> condition)
        {
            var movies = GetCached();
            var result = movies.Where(condition);
            if (result.Count() == 0)
            {
                // Try again if Radarr hasn't been queried very recently.
                movies = GetCached(TimeSpan.FromSeconds(30));
                result = movies.Where(condition);
            }
            return result;
        }
    }
}