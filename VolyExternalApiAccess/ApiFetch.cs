using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VolyExternalApiAccess.Darr.Sonarr;
using VolyExternalApiAccess.Darr.Radarr;

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

        public ApiValue RetrieveInfo(string path)
        {
            switch (type.ToUpperInvariant())
            {
                case "SONARR":
                    return RetrieveSonarr(path);
                case "RADARR":
                    return RetrieveRadarr(path);
                default:
                    throw new ArgumentException($"Invalid ApiType {type}");
            }
        }

        private ApiValue RetrieveSonarr(string path)
        {
            var api = new SonarrQuery(url, apiKey, username, password);
            var mediaInfo = api.Find(path);

            if (mediaInfo == null || mediaInfo.Series == null || mediaInfo.ParsedEpisodeInfo == null) { return null; }

            var episode = mediaInfo.Episodes?.FirstOrDefault();

            return new ApiValue()
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
            };
        }

        private ApiValue RetrieveRadarr(string path)
        {
            string directory = System.IO.Path.GetDirectoryName(path);
            string filename = System.IO.Path.GetFileName(path);

            var api = new RadarrQuery(url, apiKey, username, password);
            var mediaInfo = api.Where((x) => x.FolderName == directory && x.MovieFile.RelativePath == filename).FirstOrDefault();

            if (mediaInfo == null || mediaInfo.Title == null) { return null; }

            return new ApiValue()
            {
                SeriesTitle = mediaInfo.Title,
                Title = mediaInfo.Title,
                ImdbId = mediaInfo.ImdbId,
                TmdbId = mediaInfo.TmdbId,
                Genres = mediaInfo.Genres
            };
        }
    }
}
