using System;
using System.Collections.Generic;
using System.Text;

namespace VolyExports
{
    /// <summary>
    /// Represents a key for the pending deletions table.
    /// </summary>
    public class DeletionInfo
    {
        /// <summary>
        /// The media item to be deleted.
        /// </summary>
        public int MediaId { get; set; }
        /// <summary>
        /// The media item version to be deleted.
        /// If -1, all media files and the media item itself will be deleted.
        /// </summary>
        public int Version { get; set; }
    }
}
