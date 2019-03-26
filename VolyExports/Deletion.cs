using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VolyExports
{
    /// <summary>
    /// Represents a deleted item reported in a transaction window.
    /// </summary>
    public class Deletion
    {
        /// <summary>
        /// The table this item belonged to (MediaItem).
        /// </summary>
        public TransactionTableType Table { get; set; }
        /// <summary>
        /// The key for the table this item belonged to.
        /// </summary>
        public int Key { get; set; }

        /// <summary>
        /// Creates a typical deletion instance.
        /// </summary>
        public Deletion(TransactionTableType table, int key)
        {
            Table = table;
            Key = key;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public Deletion()
        {

        }
    }
}