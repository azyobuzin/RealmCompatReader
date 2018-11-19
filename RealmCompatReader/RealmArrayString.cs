using System;
using System.IO;
using System.Text;

namespace RealmCompatReader
{
    public class RealmArrayString
    {
        private readonly UnmanagedMemoryAccessor _accessor;
        private readonly ulong _ref;

        public RealmArrayHeader Header { get; }
        public bool Nullable { get; set; }

        public RealmArrayString(UnmanagedMemoryAccessor accessor, ulong @ref, bool nullable)
        {
            this._accessor = accessor;
            this._ref = @ref;
            this.Header = new RealmArrayHeader(accessor, @ref);
            this.Nullable = nullable;
        }

        public string this[int index]
        {
            get
            {
                var ndx = (uint)index;
                if (ndx >= (uint)this.Header.Size)
                    throw new ArgumentOutOfRangeException(nameof(index));

                // この width は 1 要素あたりのビット長ではなくバイト長
                var width = this.Header.Width;

                // 0 は null
                if (width == 0)
                    return this.Nullable ? null : "";

                var dataStart = this._ref + RealmArrayHeader.HeaderSize + width * ndx;
                var data = new byte[width];
                this._accessor.ReadArray(checked((long)dataStart), data, 0, width);

                // 最後のバイトは、ゼロ終端・パディングに何バイト使っているか（何バイト余っているか）を表す
                // xxx0 xx01 x002 0003 0004 (strings "xxx",. "xx", "x", "", realm::null())
                var zeroCount = data[width - 1];

                if (zeroCount == width)
                    return this.Nullable ? null : "";

                // ゼロ終端の前までを UTF-8 として読み出す
                var len = width - 1 - zeroCount;
                return Encoding.UTF8.GetString(data, 0, len);
            }
        }
    }
}
