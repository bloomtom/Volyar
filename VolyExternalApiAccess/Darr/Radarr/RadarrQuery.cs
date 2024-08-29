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
using System.Threading.Tasks;
using System.Threading;

namespace VolyExternalApiAccess.Darr.Radarr
{
    internal class RadarrCached
    {
        public ICollection<Movie> Movies { get; private set; } = null;
        public Stopwatch LastFilled { get; private set; } = new Stopwatch();

        private readonly SemaphoreSlim semaphore = new(1, 1);

        public async Task UpdateAsync(ICollection<Movie> movies)
        {
            if (await semaphore.WaitAsync(TimeSpan.FromSeconds(5)))
            {
                try
                {
                    Movies = movies;
                    LastFilled.Restart();
                }
                finally
                {
                    semaphore.Release();
                }
            }
        }
    }

    public class RadarrQuery : DarrQuery
    {

        private readonly TimeSpan cacheTimeout;
        private static readonly ConcurrentDictionary<string, RadarrCached> _cached = new();

        public RadarrQuery(string baseUrl, string apiKey, DarrApiVersion apiVersion, string username = null, string password = null, TimeSpan? cacheTimeout = null)
            : base(baseUrl, apiKey, apiVersion, username, password)
        {
            this.cacheTimeout = cacheTimeout ?? TimeSpan.FromMinutes(30);
            _cached.GetOrAdd(base.baseUrl, new RadarrCached());
        }

        protected virtual async Task<ApiResponse<ICollection<Movie>>> GetMoviesAsync()
        {
            return await QueryApiAsync(async (client) =>
            {
                string query = $"{baseUrl}{baseApi}movie";
                HttpResponseMessage result;
                string message = string.Empty;
                try
                {
                    result = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"{query}?apikey={apiKey}"));
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
                    var content = await result.Content.ReadAsStringAsync();
                    deserialized = Movie.FromJson(content);
                }
                catch (Exception ex)
                {
                    return new ApiResponse<ICollection<Movie>>(null, HttpStatusCode.BadRequest, $"Failed to deserialize Radarr API result with URL {query} Exception: {ex})");
                }

                return new ApiResponse<ICollection<Movie>>(deserialized, result.StatusCode);
            });
        }

        private async Task<ApiResponse<ICollection<Movie>>> GetCachedAsync(TimeSpan? invalidation = null)
        {
            invalidation ??= cacheTimeout;

            var cachedItem = _cached[baseUrl];

            if (cachedItem.Movies == null || cachedItem.LastFilled.Elapsed > invalidation)
            {
                var response = await GetMoviesAsync();
                if (response.IsSuccessStatusCode)
                {
                    await cachedItem.UpdateAsync(response.Value);
                }
                return response;
            }
            return new ApiResponse<ICollection<Movie>>(cachedItem.Movies, HttpStatusCode.OK);
        }

        /// <summary>
        /// Gets a collection of movies from the cache, or queries the cache
        /// </summary>
        /// <returns></returns>
        public async Task<ApiResponse<ICollection<Movie>>> MoviesAsync()
        {
            return await GetCachedAsync();
        }

        public async Task<ApiResponse<IEnumerable<Movie>>> WhereAsync(Func<Movie, bool> condition)
        {
            var response = await GetCachedAsync();
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return new ApiResponse<IEnumerable<Movie>>(Enumerable.Empty<Movie>(), response.StatusCode, response.ErrorDetails);
            }

            var filtered = response.Value?.Where(condition);
            if (filtered == null || !filtered.Any())
            {
                // Try again if Radarr hasn't been queried very recently.
                response = await GetCachedAsync(TimeSpan.FromSeconds(2));
                filtered = response.Value.Where(condition);
            }
            return new ApiResponse<IEnumerable<Movie>>(filtered, response.StatusCode, response.ErrorDetails);
        }
    }
}