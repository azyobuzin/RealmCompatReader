using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace RealmCompatReader
{
    public class RealmArrayBigBlobs : IRealmArray<byte[]>
    {
        private readonly RealmArray _array;

        public RealmArrayBigBlobs(ReferenceAccessor @ref)
        {
            this._array = new RealmArray(@ref);
        }

        public ReferenceAccessor Ref => this._array.Ref;

        public RealmArrayHeader Header => this._array.Header;

        public int Count => this._array.Count;

        public byte[] this[int index]
        {
            get
            {
                var blobRefValue = (ulong)this._array[index];

                // 0 は null
                if (blobRefValue == 0) return null;

                var blobArrayRef = this.Ref.NewRef(blobRefValue);
                var blobHeader = new RealmArrayHeader(blobArrayRef);

                // ContextFlag、一体何者なんだ……？
                // https://github.com/realm/realm-core/blob/v5.12.7/src/realm/array_blobs_big.hpp#L109
                if (blobHeader.ContextFlag) return null;

                var len = blobHeader.Size;
                var data = new byte[len];
                // ヘッダーの後 Size バイトが実際のデータ
                blobArrayRef.ReadBytes(RealmArrayHeader.HeaderSize, data, 0, len);
                return data;
            }
        }

        public string GetString(int index)
        {
            var bytes = this[index];
            if (bytes == null) return null;

            // null 終端なので、それを除去した範囲を UTF-8 でデコード
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length - 1);
        }

        public IEnumerator<byte[]> GetEnumerator()
        {
            var count = this.Count;
            for (var i = 0; i < count; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
