using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VolyConverter.Scanning
{
    public enum ScanType
    {
        None,
        Library,
        FilteredLibrary
    }

    public interface IScanItem
    {
        ScanType Type { get; }
        string Name { get; }
        CancellationTokenSource CancellationToken { get; }

        bool Scan();
    }

    public abstract class ScanItem : IScanItem
    {
        public ScanType Type { get; private set; }
        public string Name { get; protected set; }
        public CancellationTokenSource CancellationToken { get; private set; } = new CancellationTokenSource();

        public ScanItem(ScanType type, string name)
        {
            Type = type;
            Name = name;
        }

        public abstract bool Scan();

        public override string ToString() => Type + ":" + Name;
    }
}
