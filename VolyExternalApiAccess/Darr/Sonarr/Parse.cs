using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace VolyExternalApiAccess.Darr
{

    public class SonarrParsed
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("parsedEpisodeInfo")]
        public ParsedEpisodeInfo ParsedEpisodeInfo { get; set; }

        [JsonProperty("series")]
        public Series Series { get; set; }

        [JsonProperty("episodes")]
        public List<Episode> Episodes { get; set; }

        public static SonarrParsed FromJson(string json) => JsonConvert.DeserializeObject<SonarrParsed>(json, Converter.Settings);
    }

    public class Episode
    {
        [JsonProperty("seriesId")]
        public int SeriesId { get; set; }

        [JsonProperty("episodeFileId")]
        public int EpisodeFileId { get; set; }

        [JsonProperty("seasonNumber")]
        public int SeasonNumber { get; set; }

        [JsonProperty("episodeNumber")]
        public int EpisodeNumber { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("airDate")]
        public string AirDate { get; set; }

        [JsonProperty("airDateUtc")]
        public DateTime? AirDateUtc { get; set; }

        [JsonProperty("overview")]
        public string Overview { get; set; }

        [JsonProperty("hasFile")]
        public bool HasFile { get; set; }

        [JsonProperty("monitored")]
        public bool Monitored { get; set; }

        [JsonProperty("absoluteEpisodeNumber")]
        public int? AbsoluteEpisodeNumber { get; set; }

        [JsonProperty("sceneAbsoluteEpisodeNumber")]
        public int? SceneAbsoluteEpisodeNumber { get; set; }

        [JsonProperty("sceneEpisodeNumber")]
        public int? SceneEpisodeNumber { get; set; }

        [JsonProperty("sceneSeasonNumber")]
        public int? SceneSeasonNumber { get; set; }

        [JsonProperty("unverifiedSceneNumbering")]
        public bool UnverifiedSceneNumbering { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("lastSearchTime")]
        public DateTime? LastSearchTime { get; set; }

        [JsonProperty("seriesTitle")]
        public string SeriesTitle { get; set; }
    }

    public class ParsedEpisodeInfo
    {
        [JsonProperty("releaseTitle")]
        public string ReleaseTitle { get; set; }

        [JsonProperty("seriesTitle")]
        public string SeriesTitle { get; set; }

        [JsonProperty("seriesTitleInfo")]
        public SeriesTitleInfo SeriesTitleInfo { get; set; }

        [JsonProperty("quality")]
        public QualityModel Quality { get; set; }

        [JsonProperty("seasonNumber")]
        public int SeasonNumber { get; set; }

        [JsonProperty("episodeNumbers")]
        public List<int> EpisodeNumbers { get; set; }

        [JsonProperty("absoluteEpisodeNumbers")]
        public List<int> AbsoluteEpisodeNumbers { get; set; }

        [JsonProperty("specialAbsoluteEpisodeNumbers")]
        public List<decimal> SpecialAbsoluteEpisodeNumbers { get; set; }

        /// <summary>
        /// The type of this differs between Sonarr V2 and V3.
        /// The V2 type is string. The V3 type is a key value collection of [id,name].
        /// </summary>
        [JsonProperty("language")]
        public dynamic Language { get; set; }

        [JsonProperty("fullSeason")]
        public bool FullSeason { get; set; }

        [JsonProperty("isPartialSeason")]
        public bool IsPartialSeason { get; set; }

        [JsonProperty("isSeasonExtra")]
        public bool IsSeasonExtra { get; set; }

        [JsonProperty("special")]
        public bool Special { get; set; }

        [JsonProperty("releaseHash")]
        public string ReleaseHash { get; set; }

        [JsonProperty("seasonPart")]
        public int SeasonPart { get; set; }

        [JsonProperty("isDaily")]
        public bool IsDaily { get; set; }

        [JsonProperty("isAbsoluteNumbering")]
        public bool IsAbsoluteNumbering { get; set; }

        [JsonProperty("isPossibleSpecialEpisode")]
        public bool IsPossibleSpecialEpisode { get; set; }

        [JsonProperty("isPossibleSceneSeasonSpecial")]
        public bool IsPossibleSceneSeasonSpecial { get; set; }
    }

    public class SeriesTitleInfo
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("titleWithoutYear")]
        public string TitleWithoutYear { get; set; }

        [JsonProperty("year")]
        public int Year { get; set; }
    }

    public class Series
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("alternateTitles")]
        public List<SonarrAlternateTitle> AlternateTitles { get; set; }

        [JsonProperty("sortTitle")]
        public string SortTitle { get; set; }

        [JsonProperty("seasonCount")]
        public int SeasonCount { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("overview")]
        public string Overview { get; set; }

        [JsonProperty("network")]
        public string Network { get; set; }

        [JsonProperty("airTime")]
        public string AirTime { get; set; }

        [JsonProperty("images")]
        public List<MediaCover> Images { get; set; }

        [JsonProperty("seasons")]
        public List<Season> Seasons { get; set; }

        [JsonProperty("year")]
        public int Year { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("profileId")]
        public long ProfileId { get; set; }

        [JsonProperty("seasonFolder")]
        public bool SeasonFolder { get; set; }

        [JsonProperty("monitored")]
        public bool Monitored { get; set; }

        [JsonProperty("useSceneNumbering")]
        public bool UseSceneNumbering { get; set; }

        [JsonProperty("runtime")]
        public int Runtime { get; set; }

        [JsonProperty("tvdbId")]
        public int? TvdbId { get; set; }

        [JsonProperty("tvRageId")]
        public int? TvRageId { get; set; }

        [JsonProperty("tvMazeId")]
        public int? TvMazeId { get; set; }

        [JsonProperty("firstAired")]
        public DateTime? FirstAired { get; set; }

        [JsonProperty("lastInfoSync")]
        public DateTime? LastInfoSync { get; set; }

        [JsonProperty("seriesType")]
        public string SeriesType { get; set; }

        [JsonProperty("cleanTitle")]
        public string CleanTitle { get; set; }

        [JsonProperty("imdbId")]
        public string ImdbId { get; set; }

        [JsonProperty("titleSlug")]
        public string TitleSlug { get; set; }

        [JsonProperty("certification")]
        public string Certification { get; set; }

        [JsonProperty("genres")]
        public List<string> Genres { get; set; }

        [JsonProperty("tags")]
        public List<int> Tags { get; set; }

        [JsonProperty("added")]
        public DateTime Added { get; set; }

        [JsonProperty("ratings")]
        public Rating Ratings { get; set; }

        [JsonProperty("qualityProfileId")]
        public int QualityProfileId { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("rootFolderPath")]
        public string RootFolderPath { get; set; }
    }

    public class SonarrAlternateTitle
    {
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("seasonNumber")]
        public int? SeasonNumber { get; set; }
        [JsonProperty("sceneSeasonNumber")]
        public int? SceneSeasonNumber { get; set; }
    }

    public class Season
    {
        [JsonProperty("seasonNumber")]
        public int SeasonNumber { get; set; }

        [JsonProperty("monitored")]
        public bool Monitored { get; set; }

        [JsonProperty("images")]
        public List<MediaCover> Images { get; set; }
    }
}
