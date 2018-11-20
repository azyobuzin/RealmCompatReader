using System;
using System.Collections;
using System.Collections.Generic;

namespace RealmCompatReader
{
    public class RealmArray : IReadOnlyList<long>
    {
        public ReferenceAccessor Ref { get; }

        public RealmArrayHeader Header { get; }

        public RealmArray(ReferenceAccessor @ref)
        {
            this.Ref = @ref;
            this.Header = new RealmArrayHeader(@ref);
        }

        public int Count => this.Header.Size;

        public long this[int index]
        {
            get
            {
                // signed のセマンティクスに付き合いたくない
                var ndx = (uint)index;
                if (ndx >= this.Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                // https://github.com/realm/realm-core/blob/v5.12.1/src/realm/array.hpp#L2003-L2046
                ulong offset;

                switch (this.Header.Width)
                {
                    case 0:
                        return 0;
                    case 1:
                        offset = ndx >> 3;
                        return (this.Ref.ReadByte(this.DataPos(offset)) >> (int)(ndx & 7)) & 0x01;
                    case 2:
                        offset = ndx >> 2;
                        return (this.Ref.ReadByte(this.DataPos(offset)) >> ((int)(ndx & 3) << 1)) & 0x03;
                    case 4:
                        offset = ndx >> 1;
                        return (this.Ref.ReadByte(this.DataPos(offset)) >> ((int)(ndx & 1) << 2)) & 0x0F;
                    case 8:
                        return this.Ref.ReadSByte(this.DataPos(ndx));
                    case 16:
                        offset = (ulong)ndx * 2;
                        return this.Ref.ReadInt16(this.DataPos(offset));
                    case 32:
                        offset = (ulong)ndx * 4;
                        return this.Ref.ReadInt32(this.DataPos(offset));
                    case 64:
                        offset = (ulong)ndx * 8;
                        return this.Ref.ReadInt64(this.DataPos(offset));
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        private long DataPos(ulong offset)
        {
            return checked((long)(RealmArrayHeader.HeaderSize + offset));
        }

        public IEnumerator<long> GetEnumerator()
        {
            var count = this.Count;
            for (var i = 0; i < count; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
