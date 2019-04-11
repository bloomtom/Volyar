using DEnc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using VolyConverter.Conversion;
using VolyConverter.Scanning;
using VolyDatabase;

namespace VolyConverter.Plugin
{
    /// <summary>
    /// Arguments for a post-conversion plugin.
    /// </summary>
    public class PostConversionPluginArgs
    {
        /// <summary>
        /// The library the conversion was performed for.
        /// </summary>
        public ILibrary Library { get; }
        /// <summary>
        /// An open database context.
        /// </summary>
        public VolyContext Context { get; }
        /// <summary>
        /// An open database context.
        /// </summary>
        public MediaItem MediaItem { get; }
        /// <summary>
        /// The item that was converted. This will be null if no conversion will take place (e.g. MetadataOnly conversion)
        /// </summary>
        public IConversionItem ConversionItem { get; }
        /// <summary>
        /// The result of the conversion. This will be null if no conversion will take place (e.g. MetadataOnly conversion)
        /// </summary>
        public DashEncodeResult ConversionResult { get; }
        /// <summary>
        /// The type of conversion that was performed.
        /// </summary>
        public ConversionType Type { get; }
        /// <summary>
        /// The logger attached to the conversion process.
        /// </summary>
        public ILogger Log { get; }

        /// <summary>
        /// 
        /// </summary>
        public PostConversionPluginArgs(ILibrary library, VolyContext context, IConversionItem item, MediaItem mediaItem, DashEncodeResult conversionResult, ConversionType type, ILogger log)
        {
            this.Library = library;
            this.Context = context;
            this.ConversionItem = item;
            this.MediaItem = mediaItem;
            this.ConversionResult = conversionResult;
            this.Type = type;
            this.Log = log;
        }
    }
}
