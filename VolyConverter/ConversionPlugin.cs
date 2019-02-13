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
    /// <param name="args"></param>
    public delegate void ConversionPlugin(ConversionPluginArgs args);

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
        public ILibrary Library { get; private set; }
        /// <summary>
        /// An open database context.
        /// </summary>
        public VolyContext Context { get; private set; }
        /// <summary>
        /// The item that was converted.
        /// </summary>
        public IConversionItem Item { get; private set; }
        /// <summary>
        /// The result of the conversion.
        /// </summary>
        public DashEncodeResult ConversionResult { get; private set; }
        /// <summary>
        /// The type of conversion that was performed.
        /// </summary>
        public ConversionType Type { get; private set; }
        /// <summary>
        /// The logger attached to the conversion process.
        /// </summary>
        public ILogger Log { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public ConversionPluginArgs(ILibrary library, VolyContext context, IConversionItem item, DashEncodeResult conversionResult, ConversionType type, ILogger log)
        {
            this.Library = library;
            this.Context = context;
            this.Item = item;
            this.ConversionResult = conversionResult;
            this.Type = type;
            this.Log = log;
        }
    }
}
