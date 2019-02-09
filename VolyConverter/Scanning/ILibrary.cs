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
        /// Enables the library for scanning.
        /// </summary>
        bool Enable { get; }
        /// <summary>
        /// The path to source media items from.
        /// </summary>
        string OriginPath { get; }
        /// <summary>
        /// Specifies what should be done to source files after processing.
        /// </summary>
        string SourceHandling { get; }
        /// <summary>
        /// If true, transcoded media objects are deleted from the database and storage backend when the source file cannot be found.
        /// This is incompatible with SourceHandling.Delete
        /// </summary>
        bool DeleteWithSource { get; }
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
        /// <summary>
        /// A collection of web hooks to call upon conversion of an item.
        /// </summary>
        IEnumerable<WebHook> WebHooks { get; }

        string ToString();
    }
}