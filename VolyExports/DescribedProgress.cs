using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VolyExports
{
    /// <summary>
    /// Represents a progress indication with description text.
    /// </summary>
    public class DescribedProgress
    {
        /// <summary>
        /// The progress description.
        /// </summary>
        public string Description { get; private set; }
        /// <summary>
        /// The progress as a value 0-1.
        /// </summary>
        public double Progress { get; set; }

        /// <summary>
        /// Creates a typical instance.
        /// </summary>
        public DescribedProgress(string description, double progress)
        {
            Description = description;
            Progress = progress;
        }
    }
}
