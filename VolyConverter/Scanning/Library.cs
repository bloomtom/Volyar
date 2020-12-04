using DEnc;
using DEnc.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace VolyConverter.Scanning
{
    /// <summary>
    /// Represents the configuration for a library directory.
    /// </summary>
    public class Library : ILibrary
    {
        /// <summary>
        /// The library name. Should be unique.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Enables the library for scanning.
        /// </summary>
        public bool Enable { get; set; } = true;
        /// <summary>
        /// The path to source media items from.
        /// </summary>
        public string OriginPath { get; set; }
        /// <summary>
        /// Specifies what should be done to source files after processing.
        /// Supports: none, truncate, delete.
        /// </summary>
        public string SourceHandling { get; set; } = "none";
        /// <summary>
        /// If true, transcoded media objects are deleted from the database and storage backend when the source file cannot be found.<br/>
        /// This is incompatible with SourceHandling: "delete"
        /// </summary>
        public bool DeleteWithSource { get; set; } = true;
        /// <summary>
        /// The temporary path to store intermediate files when encoding.
        /// </summary>
        public string TempPath { get; set; }
        /// <summary>
        /// The file extensions allowed for uptake in a scan. Should start with a dot (.mp4, .mkv, etc.)
        /// </summary>
        public HashSet<string> ValidExtensions { get; set; }
        /// <summary>
        /// If true, audio tracks with more than two channels are downmixed into stereo.<br/>
        /// Two channel audio is more compatible with web browsers.
        /// </summary>
        public bool DownmixAudio { get; set; } = true;
        /// <summary>
        /// A collection of qualities to encode into.
        /// </summary>
        public IEnumerable<Quality> Qualities { get; set; }
        /// <summary>
        /// If not zero, the output framerate is forced to this value on encode.
        /// </summary>
        public int ForceFramerate { get; set; } = 0;

        public Library()
        {

        }
    }
}
