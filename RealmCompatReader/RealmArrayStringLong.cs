using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace RealmCompatReader
{
    public class RealmArrayStringLong : IRealmArray<string>
    {
        public bool Nullable { get; }

        private readonly RealmArray _array;
        private readonly RealmArray _offsets;
        private readonly ReferenceAccessor _blob;
        private readonly RealmArray _nulls;

        public RealmArrayStringLong(ReferenceAccessor @ref, bool nullable)
        {
            this.Nullable = nullable;

            var array = new RealmArray(@ref);
            this._array = array;

            this._offsets = new RealmArray(@ref.NewRef((ulong)array[0]));

            // ArrayBlob なので一応 Array のヘッダーを持っている
            this._blob = @ref.NewRef((ulong)array[1] + RealmArrayHeader.HeaderSize);

            if (nullable)
            {
                this._nulls = new RealmArray(@ref.NewRef((ulong)array[2]));
            }
        }

        public ReferenceAccessor Ref => this._array.Ref;

        public RealmArrayHeader Header => this._array.Header;

        public int Count => this._offsets.Count;

        public string this[int index]
        {
            get
            {
                // null なら nulls に 0 が入っている
                if (this.Nullable && this._nulls[index] == 0)
                    return null;

                // offsets[index] には index の終わりのアドレスが記録されている
                var begin = index > 0 ? this._offsets[index - 1] : 0;
                var end = this._offsets[index];

                // null 終端
                end--;

                // blob から読み出し
                var len = checked((int)(end - begin));
                var data = new byte[len];
                this._blob.ReadBytes(begin, data, 0, len);
                return Encoding.UTF8.GetString(data);
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
