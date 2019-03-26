using System;
using System.Collections.Generic;
using System.Text;

namespace VolyExports
{
    /// <summary>
    /// The type of transaction in a transaction log entry
    /// </summary>
    public enum TransactionType
    {
        /// <summary>
        /// Item was added.
        /// </summary>
        Insert = 0,
        /// <summary>
        /// Items was modified.
        /// </summary>
        Update = 1,
        /// <summary>
        /// Item was removed.
        /// </summary>
        Delete = 2
    }
}
