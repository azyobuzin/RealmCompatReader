using System.Collections;
using System.Collections.Generic;

namespace RealmCompatReader
{
    /// <summary>
    /// null を含む整数配列
    /// </summary>
    /// <remarks>
    /// null を表す値が配列の最初の要素として記録されているので、インデックスを 1 ずらして操作する。
    /// </remarks>
    public class RealmArrayIntNull : IReadOnlyList<long?>
    {
        private readonly RealmArray _array;

        public RealmArrayIntNull(ReferenceAccessor @ref)
        {
            this._array = new RealmArray(@ref);
        }

        public ReferenceAccessor Ref => this._array.Ref;

        public RealmArrayHeader Header => this._array.Header;

        // 最初の要素を除く
        public int Count => this._array.Header.Size - 1;

        public long? this[int index]
        {
            get
            {
                var value = this._array[index + 1];
                var nullValue = this._array[0];
                return value == nullValue ? default(long?) : value;
            }
        }

        public IEnumerator<long?> GetEnumerator()
        {
            var count = this.Count;
            for (var i = 0; i < count; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
