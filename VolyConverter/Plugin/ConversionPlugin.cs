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
    /// The signature for a conversion plugin which is executed before conversion is started.
    /// </summary>
    public delegate void PreConversionPluginAction(PreConversionPluginArgs args);
    /// <summary>
    /// The signature for a conversion plugin which is executed after conversion is complete but before data is comitted to the database.
    /// </summary>
    public delegate void PostConversionPluginAction(PostConversionPluginArgs args);

    /// <summary>
    /// A plugin that's run before any conversion is performed.
    /// </summary>
    public class PreConversionPlugin : IConversionPlugin
    {
        /// <summary>
        /// The plugin name.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// The action to run.
        /// </summary>
        public PreConversionPluginAction Action { get; }

        public PreConversionPlugin(string name, PreConversionPluginAction plugin)
        {
            Name = name;
            Action = plugin;
        }
    }

    /// <summary>
    /// A plugin that's run after a conversion has been completed and the database has been updated.
    /// </summary>
    public class PostConversionPlugin : IConversionPlugin
    {
        /// <summary>
        /// The plugin name.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// The action to run.
        /// </summary>
        public PostConversionPluginAction Action { get; }

        public PostConversionPlugin(string name, PostConversionPluginAction plugin)
        {
            Name = name;
            Action = plugin;
        }
    }
}
