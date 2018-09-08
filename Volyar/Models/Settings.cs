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
        public string Listen { get; set; } = "0.0.0.0";
        public int Port { get; set; } = 7014;
        public string DatabaseType { get; set; } = "sqlite";
        public string DatabaseConnection { get; set; } = $"Data Source={Path.Join(Environment.CurrentDirectory, "volyar.sqlite")};";
        public string FFmpegPath { get; set; } = "ffmpeg";
        public string FFprobePath { get; set; } = "ffprobe";
        public string Mp4BoxPath { get; set; } = "mp4box";
        public string TempPath { get; set; } = "";
        public int Parallelization { get; set; } = 2;
        public bool TruncateSource { get; set; } = true;
        public bool DeleteWithSource { get; set; } = true;
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

    public class Library
    {
        public string Name { get; set; }
        public string OriginPath { get; set; }
        public string StoragePath { get; set; }
        public HashSet<string> ValidExtensions { get; set; }
        public IEnumerable<Quality> Qualities { get; set; }
        public int ForceFramerate { get; set; } = 0;

        public Library() : this(null, null, null, null, null)
        {

        }

        public Library(string name, string originPath, string storagePath, HashSet<string> whitelist, IEnumerable<Quality> qualities)
        {
            Name = name ?? "Default";
            OriginPath = originPath ?? Environment.CurrentDirectory;
            OriginPath = storagePath ?? Environment.CurrentDirectory;
            ValidExtensions = whitelist ?? new HashSet<string>() { ".mpv", ".mp4", ".avi", ".mov", ".webm", ".ogg" };
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
                "Storage Path: " + StoragePath,
                "Whitelist: " + string.Join(" ", ValidExtensions),
                $"Qualities: \n{{{string.Join('\n', Qualities.Select(x => x.ToString()))}\n}}"
            });
        }
    }
}
