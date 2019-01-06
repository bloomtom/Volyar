﻿using System;
using System.Collections.Generic;
using System.Text;

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
        /// The name of the series this media item belongs to. If the media item is a movie, this may be the same as Name.
        /// </summary>
        public string SeriesName { get; set; }
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
        /// Generate a MediaItem from an IMediaItem.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static MediaItem Convert(IMediaItem item)
        {
            return new MediaItem()
            {
                CreateDate = item.CreateDate,
                Duration = item.Duration,
                IndexHash = item.IndexHash,
                IndexName = item.IndexName,
                LibraryName = item.LibraryName,
                MediaId = item.MediaId,
                Name = item.Name,
                SeriesName = item.SeriesName,
                SourceHash = item.SourceHash,
                SourceModified = item.SourceModified,
                SourcePath = item.SourcePath
            };
        }
    }
}