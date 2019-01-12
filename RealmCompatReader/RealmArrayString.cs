using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace RealmCompatReader
{
    public class RealmArrayString : IRealmArray<string>
    {
        public ReferenceAccessor Ref { get; }
        public RealmArrayHeader Header { get; }
        public bool Nullable { get; set; }

        public RealmArrayString(ReferenceAccessor @ref, bool nullable)
        {
            this.Ref = @ref;
            this.Header = new RealmArrayHeader(@ref);
            this.Nullable = nullable;
        }

        public int Count => this.Header.Size;

        public string this[int index]
        {
            get
            {
                var ndx = (uint)index;
                if (ndx >= (uint)this.Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                // この width は 1 要素あたりのビット長ではなくバイト長
                var width = this.Header.Width;

                // 0 は null
                if (width == 0)
                    return this.Nullable ? null : "";

                var dataStart = RealmArrayHeader.HeaderSize + width * (long)ndx;
                var data = new byte[width];
                this.Ref.ReadBytes(dataStart, data, 0, width);

                // 最後のバイトは、ゼロ終端・パディングに何バイト使っているか（何バイト余っているか）を表す
                // xxx0 xx01 x002 0003 0004 (strings "xxx",. "xx", "x", "", realm::null())
                // https://github.com/realm/realm-core/blob/v5.12.1/src/realm/array_string.hpp#L32
                var zeroCount = data[width - 1];

                if (zeroCount == width)
                    return this.Nullable ? null : "";

                // ゼロ終端の前までを UTF-8 として読み出す
                var len = width - 1 - zeroCount;
                return Encoding.UTF8.GetString(data, 0, len);
            }
        }

        public IEnumerator<string> GetEnumerator()
        {
            var count = this.Count;
            for (var i = 0; i < count; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
