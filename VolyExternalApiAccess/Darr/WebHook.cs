using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace VolyExternalApiAccess.Darr
{

    public class WebHookBody
    {
        [JsonProperty("eventType")]
        public string EventType { get; set; }

        [JsonProperty("movie")]
        public WebHookMovie Movie { get; set; }
        [JsonProperty("remoteMovie")]
        public RemoteMovie RemoteMovie { get; set; }
        [JsonProperty("release")]
        public Release Release { get; set; }
        [JsonProperty("movieFile")]
        public WebHookMoviefile MovieFile { get; set; }

        [JsonProperty("series")]
        public WebHookSeries Series { get; set; }
        [JsonProperty("episodes")]
        public List<WebHookEpisode> Episodes { get; set; }
        [JsonProperty("episodeFile")]
        public WebHookEpisodeFile EpisodeFile { get; set; }
        [JsonProperty("isUpgrade")]
        public bool? IsUpgrade { get; set; }
    }

    public class RemoteMovie
    {
        [JsonProperty("tmdbId")]
        public int TmdbId { get; set; }
        [JsonProperty("imdbId")]
        public string ImdbId { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("year")]
        public int Year { get; set; }
    }

    public class Release
    {
        [JsonProperty("quality")]
        public string Quality { get; set; }
        [JsonProperty("qualityVersion")]
        public int QualityVersion { get; set; }
        [JsonProperty("releaseGroup")]
        public string ReleaseGroup { get; set; }
        [JsonProperty("releaseTitle")]
        public string ReleaseTitle { get; set; }
        [JsonProperty("indexer")]
        public string Indexer { get; set; }
        [JsonProperty("size")]
        public long Size { get; set; }
    }

    public class Episodefile
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("relativePath")]
        public string RelativePath { get; set; }
        [JsonProperty("path")]
        public string Path { get; set; }
        [JsonProperty("quality")]
        public string Quality { get; set; }
        [JsonProperty("qualityVersion")]
        public int QualityVersion { get; set; }
    }

    public class WebHookMovie
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("releaseDate")]
        public string ReleaseDate { get; set; }
        [JsonProperty("folderPath")]
        public string FolderPath { get; set; }
    }

    public class WebHookMoviefile
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("relativePath")]
        public string RelativePath { get; set; }
        [JsonProperty("path")]
        public string Path { get; set; }
        [JsonProperty("quality")]
        public string Quality { get; set; }
        [JsonProperty("qualityVersion")]
        public int QualityVersion { get; set; }
        [JsonProperty("releaseGroup")]
        public string ReleaseGroup { get; set; }
    }

    public class WebHookSeries
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("path")]
        public string Path { get; set; }
        [JsonProperty("tvdbId")]
        public int TvdbId { get; set; }
    }

    public class WebHookEpisodeFile
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("relativePath")]
        public string RelativePath { get; set; }
        [JsonProperty("path")]
        public string Path { get; set; }
        [JsonProperty("quality")]
        public string Quality { get; set; }
        [JsonProperty("qualityVersion")]
        public int QualityVersion { get; set; }
    }

    public class WebHookEpisode
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("episodeNumber")]
        public int EpisodeNumber { get; set; }
        [JsonProperty("seasonNumber")]
        public int SeasonNumber { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("airDate")]
        public string AirDate { get; set; }
        [JsonProperty("airDateUtc")]
        public DateTime AirDateUtc { get; set; }
        [JsonProperty("quality")]
        public string Quality { get; set; }
        [JsonProperty("qualityVersion")]
        public int QualityVersion { get; set; }
    }

}
