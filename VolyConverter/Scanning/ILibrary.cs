using System.Collections.Generic;
using DEnc;

namespace VolyConverter.Scanning
{
    public interface ILibrary
    {
        int ForceFramerate { get; }
        string Name { get; }
        string OriginPath { get; }
        IEnumerable<Quality> Qualities { get; }
        string TempPath { get; }
        HashSet<string> ValidExtensions { get; }

        string ToString();
    }
}