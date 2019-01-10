using DEnc;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace VolyConverter.Conversion
{
    /// <summary>
    /// A json exportable view of a ConversionItem.
    /// </summary>
    public class ExportableConversionItem : IExportableConversionItem
    {
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
        public ImmutableHashSet<IQuality> Quality { get; protected set; }
        /// <summary>
        /// Override the media item's framerate. If zero the original framerate is used.
        /// </summary>
        public int Framerate { get; protected set; }
        /// <summary>
        /// The date and time this object was created.
        /// </summary>
        public DateTime CreateTime { get; protected set; } = DateTime.UtcNow;

        /// <summary>
        /// A collection of progress indicators for conversion steps on this object.
        /// </summary>
        public IEnumerable<DescribedProgress> Progress { get; protected set; } = Enumerable.Empty<DescribedProgress>();

        /// <summary>
        /// Error text which may be displayed if conversion fails.
        /// </summary>
        public string ErrorText { get; protected set; }

        /// <summary>
        /// Returns a deep copy of a given value deriving from IExportableConversionItem.
        /// </summary>
        public static ExportableConversionItem Copy(IExportableConversionItem value)
        {
            return new ExportableConversionItem()
            {
                SourcePath = value.SourcePath,
                OutputPath = value.OutputPath,
                OutputBaseFilename = value.OutputBaseFilename,
                Framerate = value.Framerate,
                Quality = value.Quality,
                CreateTime = value.CreateTime,
                Progress = value.Progress
            };
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
        public Action<IConversionItem, DashEncodeResult> CompletionAction { get; private set; }

        /// <summary>
        /// An action to perform upon conversion failure.
        /// </summary>
        public Action<Exception> ErrorAction { get; private set; }

        public new IEnumerable<DescribedProgress> Progress { get; set; } = Enumerable.Empty<DescribedProgress>();

        /// <summary>
        /// Error text which may be displayed if conversion fails.
        /// </summary>
        public new string ErrorText { get; set; }

        public ConversionItem(string sourcePath, string destination, string outputBaseFilename, HashSet<IQuality> quality, int framerate, Action<IConversionItem, DashEncodeResult> completionAction, Action<Exception> errorAction)
        {
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
