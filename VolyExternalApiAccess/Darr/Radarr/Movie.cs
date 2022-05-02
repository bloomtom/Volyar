using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace VolyExternalApiAccess.Darr
{
    public class Movie
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("alternativeTitles")]
        public List<RadarrAlternativeTitle> AlternativeTitles { get; set; }

        [JsonProperty("secondaryYearSourceId")]
        public long SecondaryYearSourceId { get; set; }

        [JsonProperty("sortTitle")]
        public string SortTitle { get; set; }

        [JsonProperty("sizeOnDisk")]
        public long SizeOnDisk { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("overview")]
        public string Overview { get; set; }

        [JsonProperty("images")]
        public List<MediaCover> Images { get; set; }

        [JsonProperty("downloaded")]
        public bool Downloaded { get; set; }

        [JsonProperty("year")]
        public long Year { get; set; }

        [JsonProperty("hasFile")]
        public bool HasFile { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("profileId")]
        public long ProfileId { get; set; }

        [JsonProperty("pathState")]
        public string PathState { get; set; }

        [JsonProperty("monitored")]
        public bool Monitored { get; set; }

        [JsonProperty("minimumAvailability")]
        public string MinimumAvailability { get; set; }

        [JsonProperty("isAvailable")]
        public bool IsAvailable { get; set; }

        [JsonProperty("folderName")]
        public string FolderName { get; set; }

        [JsonProperty("runtime")]
        public int Runtime { get; set; }

        [JsonProperty("lastInfoSync")]
        public DateTime? LastInfoSync { get; set; }

        [JsonProperty("cleanTitle")]
        public string CleanTitle { get; set; }

        [JsonProperty("imdbId")]
        public string ImdbId { get; set; }

        [JsonProperty("tmdbId")]
        public int? TmdbId { get; set; }

        [JsonProperty("titleSlug")]
        public string TitleSlug { get; set; }

        [JsonProperty("genres")]
        public List<string> Genres { get; set; }

        [JsonProperty("tags")]
        public List<int> Tags { get; set; }

        [JsonProperty("added")]
        public DateTime Added { get; set; }

        [JsonProperty("ratings")]
        public Rating Rating { get; set; }

        [JsonProperty("movieFile")]
        public MovieFile MovieFile { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        public static List<Movie> FromJson(string json) => JsonConvert.DeserializeObject<List<Movie>>(json, Converter.Settings);
    }

    public class RadarrAlternativeTitle
    {
        [JsonProperty("sourceType")]
        public string SourceType { get; set; }

        [JsonProperty("movieId")]
        public long MovieId { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("sourceId")]
        public long SourceId { get; set; }

        [JsonProperty("votes")]
        public long Votes { get; set; }

        [JsonProperty("voteCount")]
        public long VoteCount { get; set; }

        [JsonProperty("language")]
        public dynamic Language { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }
    }

    public class MovieFile
    {
        [JsonProperty("movieId")]
        public int MovieId { get; set; }

        [JsonProperty("relativePath")]
        public string RelativePath { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }

        [JsonProperty("dateAdded")]
        public DateTime DateAdded { get; set; }

        [JsonProperty("quality")]
        public MovieFileQuality Quality { get; set; }

        [JsonProperty("edition")]
        public string Edition { get; set; }

        [JsonProperty("mediaInfo")]
        public MediaInfo MediaInfo { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("releaseGroup", NullValueHandling = NullValueHandling.Ignore)]
        public string ReleaseGroup { get; set; }

        [JsonProperty("sceneName", NullValueHandling = NullValueHandling.Ignore)]
        public string SceneName { get; set; }
    }

    public class MediaInfo
    {
        [JsonProperty("containerFormat")]
        public string ContainerFormat { get; set; }

        [JsonProperty("videoFormat")]
        public string VideoFormat { get; set; }

        [JsonProperty("videoCodecID")]
        public string VideoCodecId { get; set; }

        [JsonProperty("videoProfile")]
        public string VideoProfile { get; set; }

        [JsonProperty("videoBitrate")]
        public int VideoBitrate { get; set; }

        [JsonProperty("videoBitDepth")]
        public int VideoBitDepth { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("audioFormat")]
        public string AudioFormat { get; set; }

        [JsonProperty("audioBitrate")]
        public int AudioBitrate { get; set; }

        [JsonProperty("runTime")]
        public TimeSpan RunTime { get; set; }

        [JsonProperty("audioStreamCount")]
        public int AudioStreamCount { get; set; }

        [JsonProperty("audioChannels")]
        public double AudioChannels { get; set; }

        [JsonProperty("audioChannelPositions")]
        public string AudioChannelPositions { get; set; }

        [JsonProperty("audioChannelPositionsText")]
        public string AudioChannelPositionsText { get; set; }

        [JsonProperty("audioProfile")]
        public string AudioProfile { get; set; }

        [JsonProperty("videoFps")]
        public decimal VideoFps { get; set; }

        [JsonProperty("audioLanguages")]
        public string AudioLanguages { get; set; }

        [JsonProperty("subtitles")]
        public string Subtitles { get; set; }

        [JsonProperty("scanType")]
        public string ScanType { get; set; }

        [JsonProperty("schemaRevision")]
        public long SchemaRevision { get; set; }
    }

    public class MovieFileQuality
    {
        [JsonProperty("quality")]
        public Quality Quality { get; set; }

        [JsonProperty("customFormats")]
        public List<object> CustomFormats { get; set; }

        [JsonProperty("revision")]
        public Revision Revision { get; set; }
    }
}
