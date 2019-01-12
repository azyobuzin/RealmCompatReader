using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RealmCompatReader
{
    /// <summary>
    /// float とか double とか、データをそのまま詰めるだけの配列
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RealmBasicArray<T> : IRealmArray<T>
        where T : struct
    {
        public ReferenceAccessor Ref { get; }

        public RealmArrayHeader Header { get; }

        public RealmBasicArray(ReferenceAccessor @ref)
        {
            this.Ref = @ref;
            this.Header = new RealmArrayHeader(@ref);
        }

        public int Count => this.Header.Size;

        public T this[int index]
        {
            get
            {
                var ndx = (uint)index;
                if (ndx >= this.Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                var offset = RealmArrayHeader.HeaderSize
                    + Marshal.SizeOf<T>() * index;
                return this.Ref.Read<T>(offset);
            }
        }

        private long DataPos(ulong offset)
        {
            return checked((long)(RealmArrayHeader.HeaderSize + offset));
        }

        public IEnumerator<T> GetEnumerator()
        {
            var count = this.Count;
            for (var i = 0; i < count; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
