using DEnc;
using System;
using System.Collections.Generic;
using System.Text;

namespace VolyConverter.Scanning
{
    public class Library : ILibrary
    {
        public string Name { get; set; }
        public string OriginPath { get; set; }
        public string TempPath { get; set; }
        public HashSet<string> ValidExtensions { get; set; }
        public IEnumerable<Quality> Qualities { get; set; }
        public int ForceFramerate { get; set; } = 0;

        public Library()
        {

        }
    }
}
