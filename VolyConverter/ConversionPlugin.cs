using DEnc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using VolyConverter.Conversion;
using VolyConverter.Scanning;
using VolyDatabase;

namespace VolyConverter
{
    /// <summary>
    /// The signature for a conversion plugin which is executed after conversion is complete but before data is comitted to the database.
    /// </summary>
    public delegate void ConversionPluginAction(ConversionPluginArgs args);

    public class ConversionPlugin
    {
        /// <summary>
        /// The plugin name.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// The action to run.
        /// </summary>
        public ConversionPluginAction Action { get; }

        public ConversionPlugin(string name, ConversionPluginAction plugin)
        {
            Name = name;
            Action = plugin;
        }
    }
    

    /// <summary>
    /// The type of conversion that was performed.
    /// </summary>
    public enum ConversionType
    {
        None,
        Conversion,
        Reconversion
    }

    /// <summary>
    /// Arguments for a conversion plugin.
    /// </summary>
    public class ConversionPluginArgs
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
        /// The item that was converted.
        /// </summary>
        public IConversionItem ConversionItem { get; }
        /// <summary>
        /// The result of the conversion.
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
        public ConversionPluginArgs(ILibrary library, VolyContext context, IConversionItem item, MediaItem mediaItem, DashEncodeResult conversionResult, ConversionType type, ILogger log)
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
