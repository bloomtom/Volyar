﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace VolyExternalApiAccessTests.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("VolyExternalApiAccessTests.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [
        ///  {
        ///    &quot;title&quot;: &quot;My Movie&quot;,
        ///    &quot;alternativeTitles&quot;: [
        ///      {
        ///        &quot;sourceType&quot;: &quot;tmdb&quot;,
        ///        &quot;movieId&quot;: 16,
        ///        &quot;title&quot;: &quot;Movie&quot;,
        ///        &quot;sourceId&quot;: 123000,
        ///        &quot;votes&quot;: 0,
        ///        &quot;voteCount&quot;: 0,
        ///        &quot;language&quot;: &quot;english&quot;,
        ///        &quot;id&quot;: 46
        ///      },
        ///      {
        ///        &quot;sourceType&quot;: &quot;tmdb&quot;,
        ///        &quot;movieId&quot;: 16,
        ///        &quot;title&quot;: &quot;Please Reconsider&quot;,
        ///        &quot;sourceId&quot;: 123400,
        ///        &quot;votes&quot;: 0,
        ///        &quot;voteCount&quot;: 0,
        ///        &quot;language&quot;: &quot;english&quot;,
        ///        &quot;id&quot;:  [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string RadarrV2ApiResult {
            get {
                return ResourceManager.GetString("RadarrV2ApiResult", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [{
        ///        &quot;title&quot;: &quot;My Movie&quot;,
        ///        &quot;alternativeTitles&quot;: [{
        ///                &quot;sourceType&quot;: &quot;tmdb&quot;,
        ///                &quot;movieId&quot;: 16,
        ///                &quot;title&quot;: &quot;Movie&quot;,
        ///                &quot;sourceId&quot;: 123000,
        ///                &quot;votes&quot;: 0,
        ///                &quot;voteCount&quot;: 0,
        ///                &quot;language&quot;: {
        ///                    &quot;id&quot;: 1,
        ///                    &quot;name&quot;: &quot;English&quot;
        ///                },
        ///                &quot;id&quot;: 46
        ///            }, {
        ///                &quot;sourceType&quot;: &quot;tmdb&quot;,
        ///                &quot;movieId&quot;: 16,
        ///      [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string RadarrV3ApiResult {
            get {
                return ResourceManager.GetString("RadarrV3ApiResult", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {
        ///  &quot;parsedEpisodeInfo&quot;: {
        ///    &quot;releaseTitle&quot;: &quot;My.Show.S01E13.(Part.2)&quot;,
        ///    &quot;seriesTitle&quot;: &quot;My Show&quot;,
        ///    &quot;seriesTitleInfo&quot;: {
        ///      &quot;title&quot;: &quot;My Show&quot;,
        ///      &quot;titleWithoutYear&quot;: &quot;My Show&quot;,
        ///      &quot;year&quot;: 0
        ///    },
        ///    &quot;quality&quot;: {
        ///      &quot;quality&quot;: {
        ///        &quot;id&quot;: 1,
        ///        &quot;name&quot;: &quot;SDTV&quot;,
        ///        &quot;source&quot;: &quot;television&quot;,
        ///        &quot;resolution&quot;: 480
        ///      },
        ///      &quot;revision&quot;: {
        ///        &quot;version&quot;: 1,
        ///        &quot;real&quot;: 0
        ///      }
        ///    },
        ///    &quot;seasonNumber&quot;: 1,
        ///    &quot;episodeNumbers&quot;: [
        ///      13
        ///    ],
        ///    &quot;absoluteEp [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string SonarrV2ApiResult {
            get {
                return ResourceManager.GetString("SonarrV2ApiResult", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {
        ///  &quot;parsedEpisodeInfo&quot;: {
        ///    &quot;releaseTitle&quot;: &quot;My.Show.S01E01&quot;,
        ///    &quot;seriesTitle&quot;: &quot;My Show&quot;,
        ///    &quot;seriesTitleInfo&quot;: {
        ///      &quot;title&quot;: &quot;My Show&quot;,
        ///      &quot;titleWithoutYear&quot;: &quot;My Show&quot;,
        ///      &quot;year&quot;: 0
        ///    },
        ///    &quot;quality&quot;: {
        ///      &quot;quality&quot;: {
        ///        &quot;id&quot;: 1,
        ///        &quot;name&quot;: &quot;HDTV-720p&quot;,
        ///        &quot;source&quot;: &quot;television&quot;,
        ///        &quot;resolution&quot;: 720
        ///      },
        ///      &quot;revision&quot;: {
        ///        &quot;version&quot;: 1,
        ///        &quot;real&quot;: 0,
        ///        &quot;isRepack&quot;: false
        ///      }
        ///    },
        ///    &quot;seasonNumber&quot;: 1,
        ///    &quot;epis [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string SonarrV3ApiResult {
            get {
                return ResourceManager.GetString("SonarrV3ApiResult", resourceCulture);
            }
        }
    }
}
