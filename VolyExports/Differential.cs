﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VolyExports
{
    /// <summary>
    /// Contains the entire summary of a transaction log differential.
    /// </summary>
    public class Differential
    {
        /// <summary>
        /// The newest available key from the point this differential was created.
        /// If you're tracking the newest changes, this is the key you want to pass back for your next sync operation.
        /// </summary>
        public long CurrentKey { get; private set; }
        /// <summary>
        /// The media items deleted in this differential window.
        /// </summary>
        public IEnumerable<Deletion> Deletions { get; private set; }
        /// <summary>
        /// The media items added in this differential window.
        /// </summary>
        public IEnumerable<MediaItem> Additions { get; private set; }
        /// <summary>
        /// The media items modified in this differential window.
        /// </summary>
        public IEnumerable<MediaItem> Modifications { get; private set; }

        /// <summary>
        /// Creates a new full Differential instance.
        /// </summary>
        public Differential(long currentKey, IEnumerable<Deletion> deletions, IEnumerable<IMediaItem> additons, IEnumerable<IMediaItem> modifications)
        {
            CurrentKey = currentKey;
            Deletions = deletions;
            Additions = additons.Select(x => MediaItem.Convert(x));
            Modifications = modifications.Select(x => MediaItem.Convert(x));
        }
    }
}
