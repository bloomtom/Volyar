using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Volyar.Media.Scanning
{
    public enum ScanType
    {
        None,
        Library
    }

    public interface IScanItem
    {
        ScanType Type { get; }
        string Name { get; }

        bool Scan();
    }

    public abstract class ScanItem : IScanItem
    {
        public ScanType Type { get; private set; }
        public string Name { get; private set; }

        public ScanItem(ScanType type, string name)
        {
            Type = type;
            Name = name;
        }

        public abstract bool Scan();

        public override string ToString() => Type + ":" + Name;
    }
}
