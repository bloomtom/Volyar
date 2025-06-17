using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VolyExternalApiAccess.Darr.Sonarr;
using VolyExternalApiAccess.Darr.Radarr;
using VolyExternalApiAccess.Darr;
using System.Threading.Tasks;

namespace VolyExternalApiAccess
{
    public class ApiFetch
    {
        private readonly string type;
        private readonly string url;
        private readonly string apiKey;
        private readonly string username;
        private readonly string password;

        public ApiFetch(string type, string url, string apiKey, string username, string password)
        {
            this.type = type;
            this.url = url;
            this.apiKey = apiKey;
            this.username = username;
            this.password = password;
        }

        public async Task<ApiResponse<ApiValue>> RetrieveInfoAsync(string path)
        {
            return type.ToUpperInvariant() switch
            {
                "SONARR" => await RetrieveSonarr(path),
                "RADARR" => await RetrieveRadarr(path),
                _ => throw new ArgumentException($"Invalid ApiType {type}"),
            };
        }

        public async Task<DarrApiVersion> RetrieveVersionAsync()
        {
            return await DarrQuery.QueryApiVersionAsync(url, apiKey, username, password);
        }

        private async Task<ApiResponse<ApiValue>> RetrieveSonarr(string path)
        {
            var apiVersion = await DarrQuery.QueryApiVersionAsync(url, apiKey, username, password);
            var api = new SonarrQuery(url, apiKey, apiVersion, username: username, password: password);
            var apiResponse = await api.FindAsync(path);

            if (!apiResponse.IsSuccessStatusCode || apiResponse.Value?.Series == null || apiResponse.Value.ParsedEpisodeInfo == null)
            {
                return new ApiResponse<ApiValue>(null, apiResponse.StatusCode, apiResponse.ErrorDetails);
            }

            var mediaInfo = apiResponse.Value;
            var episode = mediaInfo.Episodes?.FirstOrDefault();

            return new ApiResponse<ApiValue>(new ApiValue()
            {
                SeriesTitle = mediaInfo.Series.Title ?? mediaInfo.ParsedEpisodeInfo?.SeriesTitle ?? mediaInfo.Title,
                Title = episode?.Title,
                SeasonNumber = mediaInfo.ParsedEpisodeInfo.SeasonNumber != 0 ? mediaInfo.ParsedEpisodeInfo.SeasonNumber : (episode?.SeasonNumber ?? 0),
                EpisodeNumber = mediaInfo.ParsedEpisodeInfo.EpisodeNumbers?.Count > 0 ? mediaInfo.ParsedEpisodeInfo.EpisodeNumbers.First() : episode?.EpisodeNumber ?? 0,
                AbsoluteEpisodeNumber = mediaInfo.ParsedEpisodeInfo.IsAbsoluteNumbering && mediaInfo.ParsedEpisodeInfo?.AbsoluteEpisodeNumbers.Count > 0 ? mediaInfo.ParsedEpisodeInfo.AbsoluteEpisodeNumbers.First() : 0,
                ImdbId = mediaInfo.Series.ImdbId,
                TvdbId = mediaInfo.Series.TvdbId,
                TvMazeId = mediaInfo.Series.TvMazeId,
                Genres = mediaInfo.Series.Genres
            }, apiResponse.StatusCode, apiResponse.ErrorDetails);
        }

        private async Task<ApiResponse<ApiValue>> RetrieveRadarr(string path)
        {
            string directory = System.IO.Path.GetDirectoryName(path);
            string filename = System.IO.Path.GetFileName(path);

            var apiVersion = await DarrQuery.QueryApiVersionAsync(url, apiKey, username, password);
            var api = new RadarrQuery(url, apiKey, apiVersion, username: username, password: password);
            var filtered = await api.WhereAsync((x) => x.FolderName == directory && x.MovieFile.RelativePath == filename);

            if (filtered.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return new ApiResponse<ApiValue>(null, filtered.StatusCode, filtered.ErrorDetails);
            }

            var mediaInfo = filtered.Value.FirstOrDefault();
            if (mediaInfo == null || mediaInfo.Title == null)
            {
                return new ApiResponse<ApiValue>(null, System.Net.HttpStatusCode.BadRequest, $"Could not find the media item. Directory: {directory} Filename: {filename}");
            }

            return new ApiResponse<ApiValue>(new ApiValue()
            {
                SeriesTitle = mediaInfo.Title,
                Title = mediaInfo.Title,
                ImdbId = mediaInfo.ImdbId,
                TmdbId = mediaInfo.TmdbId,
                Genres = mediaInfo.Genres
            }, System.Net.HttpStatusCode.OK);
        }
    }
}
