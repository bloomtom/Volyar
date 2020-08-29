using DEnc;
using DEnc.Models.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VolyExports;

namespace VolyConverter.Conversion
{
    /// <summary>
    /// A json exportable view of a ConversionItem.
    /// </summary>
    public class ExportableConversionItem : IExportableConversionItem
    {
        /// <summary>
        /// The library this media item belongs to.
        /// </summary>
        public string Library { get; protected set; }
        /// <summary>
        /// If known, the series name this media item belongs to.
        /// </summary>
        public string Series { get; set; }
        /// <summary>
        /// If known, the title of this media item.
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// The file to convert. This variable is used for Equals/GetHashCode.
        /// </summary>
        public string SourcePath { get; protected set; }
        /// <summary>
        /// A description of the storage backend used for files emitted from conversion.
        /// </summary>
        public string OutputPath { get; protected set; }
        /// <summary>
        /// The base filename for the output. Ensure this is unique as collisions will result in undefined behavior.
        /// </summary>
        public string OutputBaseFilename { get; protected set; }
        /// <summary>
        /// The quality matrix to encode into.
        /// </summary>
        public IEnumerable<IQuality> Quality { get; protected set; }
        /// <summary>
        /// Override the media item's framerate. If zero the original framerate is used.
        /// </summary>
        public int Framerate { get; protected set; }
        /// <summary>
        /// The ffmpeg tune type to use for encoding.
        /// </summary>
        public Tune Tune { get; set; }
        /// <summary>
        /// The date and time this object was created.
        /// </summary>
        public DateTime CreateTime { get; protected set; } = DateTime.UtcNow;

        /// <summary>
        /// A collection of progress indicators for conversion steps on this object.
        /// </summary>
        public IEnumerable<DescribedProgress> Progress { get; set; } = Enumerable.Empty<DescribedProgress>();

        /// <summary>
        /// Error text which may be displayed if conversion fails.
        /// </summary>
        public string ErrorReason { get; set; }

        /// <summary>
        /// Detailed error information, may contain a stack trace or log entry series.
        /// </summary>
        public string ErrorDetail { get; set; }

        public ExportableConversionItem()
        {
        }

        public ExportableConversionItem(string series, string title, string sourcePath)
        {
            Series = series;
            Title = title;
            SourcePath = sourcePath;
        }
    }

    /// <summary>
    /// An object for storing the definition and callbacks for a conversion routine.
    /// </summary>
    public class ConversionItem : ExportableConversionItem, IConversionItem
    {
        /// <summary>
        /// An action to perform upon conversion success.
        /// </summary>
        [JsonIgnore]
        public Action<IConversionItem, DashEncodeResult> CompletionAction { get; private set; }

        /// <summary>
        /// An action to perform upon conversion failure.
        /// </summary>
        [JsonIgnore]
        public Action<Exception> ErrorAction { get; private set; }

        [JsonIgnore]
        public CancellationTokenSource CancellationToken { get; private set; } = new CancellationTokenSource();

        public ConversionItem(string libraryName, string seriesName, string title, string sourcePath, string destination, string outputBaseFilename,
            HashSet<IQuality> quality, int framerate,
            Action<IConversionItem, DashEncodeResult> completionAction, Action<Exception> errorAction)
        {
            Library = libraryName;
            Series = seriesName;
            Title = title;
            SourcePath = sourcePath;
            OutputPath = destination;
            OutputBaseFilename = outputBaseFilename;
            Quality = quality.ToImmutableHashSet();
            Framerate = framerate;
            CompletionAction = completionAction;
            ErrorAction = errorAction;
        }

        public override bool Equals(object obj)
        {
            return SourcePath.Equals(obj);
        }

        public override int GetHashCode()
        {
            return SourcePath.GetHashCode();
        }

        public override string ToString()
        {
            return SourcePath;
        }

        public bool Equals(IConversionItem other)
        {
            return SourcePath.Equals(other.SourcePath);
        }
    }
}
