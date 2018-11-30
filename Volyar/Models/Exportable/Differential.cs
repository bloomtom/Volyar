using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Volyar.Models.Exportable
{
    public class Differential
    {
        public long CurrentKey { get; private set; }
        public IEnumerable<Deletion> Deletions { get; private set; }
        public IEnumerable<IMediaItem> Additions { get; private set; }
        public IEnumerable<IMediaItem> Modifications { get; private set; }

        public Differential(long currentKey, IEnumerable<Deletion> deletions, IEnumerable<IMediaItem> additons, IEnumerable<IMediaItem> modifications)
        {
            CurrentKey = currentKey;
            Deletions = deletions;
            Additions = additons;
            Modifications = modifications;
        }
    }
}
