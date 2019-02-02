using DEnc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Volyar.Models
{
    public class VSettings
    {
        /// <summary>
        /// The IP address to listen on. Use 0.0.0.0 to listen on all interfaces.
        /// </summary>
        public string Listen { get; set; } = "0.0.0.0";
        /// <summary>
        /// The port to listen on for the web UI and APIs.
        /// </summary>
        public int Port { get; set; } = 7014;
        /// <summary>
        /// The web UI and API base path.
        /// </summary>
        public string BasePath { get; set; } = "/voly";
        /// <summary>
        /// The type of database to use. Accepts: temp, sqlite, sqlserver, mysql.
        /// </summary>
        public string DatabaseType { get; set; } = "sqlite";
        /// <summary>
        /// The database connection to use.
        /// </summary>
        public string DatabaseConnection { get; set; } = "Data Source=\"volyar.sqlite\";";
        /// <summary>
        /// An absolute or relative path to an ffmpeg executable.
        /// </summary>
        public string FFmpegPath { get; set; } = "ffmpeg";
        /// <summary>
        /// An absolute or relative path to an ffprobe executable.
        /// </summary>
        public string FFprobePath { get; set; } = "ffprobe";
        /// <summary>
        /// An absolute or relative path to an mp4box executable.
        /// </summary>
        public string Mp4BoxPath { get; set; } = "mp4box";
        /// <summary>
        /// The temp path to use. If none is given, the working directory is used to store temp files.
        /// </summary>
        public string TempPath { get; set; } = "";
        /// <summary>
        /// The number of media files to process at once.
        /// </summary>
        public int Parallelization { get; set; } = 2;
        /// <summary>
        /// The number of items to keep in the complete/error queue.
        /// </summary>
        public int CompleteQueueLength { get; set; } = 100;
        /// <summary>
        /// A collection of libraries to watch.
        /// </summary>
        public IEnumerable<Library> Libraries { get; set; }

        public VSettings() : this(null)
        {
            
        }

        public VSettings(IEnumerable<Library> libraries)
        {
            Libraries = libraries ?? new List<Library>() { new Library() };
        }

        public override string ToString()
        {
            return string.Join('\n', new string[] {
                $"Listen: {Listen}:{Port}",
                $"Libraries: \n{{{string.Join('\n', Libraries.Select(x => x.ToString()))}\n}}"
            });
        }
    }

    /// <summary>
    /// Represents the configuration for a library directory, including the storage backend and encoding settings.
    /// </summary>
    public class Library : VolyConverter.Scanning.Library, VolyConverter.Scanning.ILibrary
    {
        /// <summary>
        /// The storage backend setting to use for this library.
        /// </summary>
        public StorageSettings StorageBackend { get; set; } = new StorageSettings();

        public Library() : this(null, null, null, null, null)
        {

        }

        public Library(string name, string originPath, string tempPath, HashSet<string> whitelist, IEnumerable<Quality> qualities)
        {
            Name = name ?? "Default";
            OriginPath = originPath ?? Environment.CurrentDirectory;
            TempPath = tempPath ?? Environment.CurrentDirectory;
            ValidExtensions = whitelist ?? new HashSet<string>() { ".mpv", ".mp4", ".mkv", ".avi", ".mov", ".webm", ".ogg" };
            Qualities = qualities ?? new List<Quality>()
            {
                new Quality(),
                new Quality()
                {
                    Width = 1280,
                    Height = 720,
                    Bitrate = 2500
                },
                new Quality()
                {
                    Width = 1024,
                    Height = 568,
                    Bitrate = 1500
                },
            };
        }

        public override string ToString()
        {
            return string.Join('\n', new string[] {
                "Name: " + Name,
                "Origin Path: " + OriginPath,
                "Storage Path: " + StorageBackend,
                "Whitelist: " + string.Join(" ", ValidExtensions),
                $"Qualities: \n{{{string.Join('\n', Qualities.Select(x => x.ToString()))}\n}}"
            });
        }
    }
}
