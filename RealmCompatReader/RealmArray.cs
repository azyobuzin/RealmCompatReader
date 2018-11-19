using System;
using System.IO;

namespace RealmCompatReader
{
    public class RealmArray
    {
        private readonly UnmanagedMemoryAccessor _accessor;
        private readonly ulong _ref;

        public RealmArrayHeader Header { get; }

        public RealmArray(UnmanagedMemoryAccessor accessor, ulong @ref)
        {
            this._accessor = accessor;
            this._ref = @ref;
            this.Header = new RealmArrayHeader(accessor, @ref);
        }

        public long this[int index]
        {
            get
            {
                // signed のセマンティクスに付き合いたくない
                var ndx = (uint)index;
                if (ndx >= this.Header.Size)
                    throw new ArgumentOutOfRangeException(nameof(index));

                // https://github.com/realm/realm-core/blob/v5.12.1/src/realm/array.hpp#L2003-L2046
                ulong offset;

                switch (this.Header.Width)
                {
                    case 0:
                        return 0;
                    case 1:
                        offset = ndx >> 3;
                        return (this._accessor.ReadByte(this.DataPos(offset)) >> (int)(ndx & 7)) & 0x01;
                    case 2:
                        offset = ndx >> 2;
                        return (this._accessor.ReadByte(this.DataPos(offset)) >> ((int)(ndx & 3) << 1)) & 0x03;
                    case 4:
                        offset = ndx >> 1;
                        return (this._accessor.ReadByte(this.DataPos(offset)) >> ((int)(ndx & 1) << 2)) & 0x0F;
                    case 8:
                        return this._accessor.ReadSByte(this.DataPos(ndx));
                    case 16:
                        offset = (ulong)ndx * 2;
                        return this._accessor.ReadInt16(this.DataPos(offset));
                    case 32:
                        offset = (ulong)ndx * 4;
                        return this._accessor.ReadInt32(this.DataPos(offset));
                    case 64:
                        offset = (ulong)ndx * 8;
                        return this._accessor.ReadInt64(this.DataPos(offset));
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        private long DataPos(ulong offset)
        {
            return checked((long)(this._ref + RealmArrayHeader.HeaderSize + offset));
        }
    }
}
