using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using VolyConverter.Conversion;
using VolyConverter.Scanning;
using VolyDatabase;

namespace VolyConverter.Plugin
{
    public class PreConversionPluginArgs : IConversionPluginArgs
    {
        /// <summary>
        /// The library the conversion was performed for.
        /// </summary>
        public ILibrary Library { get; }
        /// <summary>
        /// An open database context.
        /// </summary>
        public MediaItem MediaItem { get; }
        /// <summary>
        /// The item that will be converted. This will be null if no conversion will take place (e.g. MetadataOnly conversion)
        /// </summary>
        public IConversionItem ConversionItem { get; }
        /// <summary>
        /// The type of conversion that is to be performed.
        /// </summary>
        public ConversionType Type { get; }
        /// <summary>
        /// The logger attached to the conversion process.
        /// </summary>
        public ILogger Log { get; }

        /// <summary>
        /// 
        /// </summary>
        public PreConversionPluginArgs(ILibrary library, IConversionItem item, MediaItem mediaItem, ConversionType type, ILogger log)
        {
            this.Library = library;
            this.ConversionItem = item;
            this.MediaItem = mediaItem;
            this.Type = type;
            this.Log = log;
        }
    }
}
