using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Volyar.Media.Conversion
{
    public class DescribedProgress
    {
        public string Description { get; private set; }
        public double Progress { get; set; }

        public DescribedProgress(string description, double progress)
        {
            Description = description;
            Progress = progress;
        }
    }
}
