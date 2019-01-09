using System.Collections.Generic;
using DEnc;

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
        /// The library name. Should be unique.
        /// </summary>
        string Name { get; }
        /// <summary>
        /// The path to source media items from.
        /// </summary>
        string OriginPath { get; }
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