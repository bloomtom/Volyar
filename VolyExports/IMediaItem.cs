﻿using System;

namespace VolyExports
{
    /// <summary>
    /// The simplest full representation of a media item.
    /// </summary>
    public interface IMediaItem
    {
        /// <summary>
        /// The date this item was created in the database.
        /// </summary>
        DateTimeOffset CreateDate { get; set; }
        /// <summary>
        /// The duration of this media file.
        /// </summary>
        TimeSpan Duration { get; set; }
        /// <summary>
        /// The md5 hash of the file located at SourcePath.
        /// </summary>
        string IndexHash { get; set; }
        /// <summary>
        /// The name of this media's index file.
        /// </summary>
        string IndexName { get; set; }
        /// <summary>
        /// The name of the library this media item belongs to.
        /// </summary>
        string LibraryName { get; set; }
        /// <summary>
        /// The database ID for this media item.
        /// </summary>
        int MediaId { get; set; }
        /// <summary>
        /// The name of this media item. If this is an episode, it is the episode name.
        /// </summary>
        string Name { get; set; }
        /// <summary>
        /// The name of the series this media item belongs to. If the media item is a movie, this may be the same as Name.
        /// </summary>
        string SeriesName { get; set; }
        /// <summary>
        /// The md5 hash of the source file this media item was transcoded from.
        /// </summary>
        string SourceHash { get; set; }
        /// <summary>
        /// The filesystem modify timestamp for the source file.
        /// </summary>
        DateTimeOffset SourceModified { get; set; }
        /// <summary>
        /// The physical path to the source file.
        /// </summary>
        string SourcePath { get; set; }
    }
}