using System.Collections;
using System.Collections.Generic;

namespace VolyConverter.Complete
{
    public interface ICompleteItems<T> : IEnumerable, IEnumerable<T>, IReadOnlyCollection<T>
    {
        void Add(T item);
    }
}