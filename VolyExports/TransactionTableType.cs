using System;
using System.Collections.Generic;
using System.Text;

namespace VolyExports
{
    /// <summary>
    /// Specifies which data table a transaction log entry is referring to.
    /// </summary>
    public enum TransactionTableType
    {
        /// <summary>
        /// Default, should not be used.
        /// </summary>
        None = 0,
        /// <summary>
        /// Specifies a media item transaction.
        /// </summary>
        MediaItem = 1
    }
}