﻿using System.Collections.Generic;
using DEnc;
using DEnc.Models;

namespace VolyConverter.Scanning
{
    /// <summary>
    /// Represents the configuration for a library directory.
    /// </summary>
    public interface ILibrary
    {
        /// <summary>
        /// If not zero, the output framerate is forced to this value on encode.
        /// </summary>
        int ForceFramerate { get; }
        /// <summary>
        /// The multiple of framerate to place keyframes. Can also be though of as the number of seconds between keyframes.<br/>
        /// This influences chunk size when streaming. Having a low multiple will increase requests per second, but will also allow faster quality level switching.
        /// </summary>
        int KeyframeMultiple { get; }
        /// <summary>
        /// The library name. Should be unique.
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Enables the library for scanning.
        /// </summary>
        bool Enable { get; }
        /// <summary>
        /// The path to source media items from.
        /// </summary>
        string OriginPath { get; }
        /// <summary>
        /// Specifies what should be done to source files after processing.
        /// Supports: none, truncate, delete.
        /// </summary>
        string SourceHandling { get; }
        /// <summary>
        /// If true, transcoded media objects are deleted from the database and storage backend when the source file cannot be found.
        /// This is incompatible with SourceHandling.Delete
        /// </summary>
        bool DeleteWithSource { get; }
        /// <summary>
        /// If true, audio tracks with more than two channels are downmixed into stereo.<br/>
        /// Two channel audio is more compatible with web browsers.
        /// </summary>
        bool DownmixAudio { get; }
        /// <summary>
        /// A collection of qualities to encode into.
        /// </summary>
        IEnumerable<Quality> Qualities { get; }
        /// <summary>
        /// The temporary path to store intermediate files when encoding.
        /// </summary>
        string TempPath { get; }
        /// <summary>
        /// The file extensions allowed for uptake in a scan. Should start with a dot (.mp4, .mkv, etc.)
        /// </summary>
        HashSet<string> ValidExtensions { get; }

        string ToString();
    }
}