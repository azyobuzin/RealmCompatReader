using System.Collections.Generic;

namespace RealmCompatReader
{
    public interface IRealmArray
    {
        ReferenceAccessor Ref { get; }
        RealmArrayHeader Header { get; }
        int Count { get; }
    }

    public interface IRealmArray<T> : IRealmArray, IReadOnlyList<T> { }
}
