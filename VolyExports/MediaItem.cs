﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace VolyExports
{
    /// <summary>
    /// The simplest full representation of a media item.
    /// </summary>
    public class MediaItem : IMediaItem
    {
        /// <summary>
        /// The date this item was created in the database.
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }
        /// <summary>
        /// The duration of this media file.
        /// </summary>
        [JsonConverter(typeof(TimeSpanConverter))]
        public TimeSpan Duration { get; set; }
        /// <summary>
        /// The md5 hash of the file located at SourcePath.
        /// </summary>
        public string IndexHash { get; set; }
        /// <summary>
        /// The name of this media's index file.
        /// </summary>
        public string IndexName { get; set; }
        /// <summary>
        /// The name of the library this media item belongs to.
        /// </summary>
        public string LibraryName { get; set; }
        /// <summary>
        /// The database ID for this media item.
        /// </summary>
        public int MediaId { get; set; }
        /// <summary>
        /// The name of this media item. If this is an episode, it is the episode name.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The season number if this was retrieved from an integration.
        /// </summary>
        public int SeasonNumber { get; set; }
        /// <summary>
        /// The episode number if this was retrieved from an integration.
        /// </summary>
        public int EpisodeNumber { get; set; }
        /// <summary>
        ///  The media variant version of this item.
        /// </summary>
        public int Version { get; set; }
        /// <summary>
        /// The absolute episode number if this was retrieved from an integration.
        /// </summary>
        public int AbsoluteEpisodeNumber { get; set; }
        /// <summary>
        /// The name of the series this media item belongs to. If the media item is a movie, this may be the same as Name.
        /// </summary>
        public string SeriesName { get; set; }
        /// <summary>
        /// The ID for this series on IMDB if this was retrieved from an integration.
        /// </summary>
        public string ImdbId { get; set; }

        /// <summary>
        /// The ID for this series on TheTVDB if this was retrieved from an integration.
        /// </summary>
        public string TvdbId { get; set; }

        /// <summary>
        /// The ID for this series on The Movie Database if this was retrieved from an integration.
        /// </summary>
        public string TmdbId { get; set; }

        /// <summary>
        /// The ID for this series on TVMAZE if this was retrieved from an integration.
        /// </summary>
        public string TvmazeId { get; set; }
        /// <summary>
        /// The md5 hash of the source file this media item was transcoded from.
        /// </summary>
        public string SourceHash { get; set; }
        /// <summary>
        /// The filesystem modify timestamp for the source file.
        /// </summary>
        public DateTimeOffset SourceModified { get; set; }
        /// <summary>
        /// The physical path to the source file.
        /// </summary>
        public string SourcePath { get; set; }
        /// <summary>
        /// A serialized key/value store of media file metadata.
        /// </summary>
        public string Metadata { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public MediaItem() { }
    }
}
