using DEnc;
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
        /// The path to source media items from.
        /// </summary>
        public string OriginPath { get; set; }
        /// <summary>
        /// The temporary path to store intermediate files when encoding.
        /// </summary>
        public string TempPath { get; set; }
        /// <summary>
        /// The file extensions allowed for uptake in a scan. Should start with a dot (.mp4, .mkv, etc.)
        /// </summary>
        public HashSet<string> ValidExtensions { get; set; }
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
