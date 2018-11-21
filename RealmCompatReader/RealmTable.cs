using System;

namespace RealmCompatReader
{
    public class RealmTable
    {
        public ReferenceAccessor Ref { get; }
        public TableSpec Spec { get; }
        public RealmArray Columns { get; }

        public RealmTable(ReferenceAccessor @ref)
        {
            this.Ref = @ref;

            var tableArray = new RealmArray(@ref);
            this.Spec = new TableSpec(@ref.NewRef((ulong)tableArray[0]));
            this.Columns = new RealmArray(@ref.NewRef((ulong)tableArray[1]));
        }

        /// <summary>
        /// Indexed カラムは Columns の中で 2 要素使用するので、
        /// カラムデータが入った B+ 木への参照を持つ要素のインデックスを計算する
        /// </summary>
        private int GetColumnBpTreeIndex(int columnIndex)
        {
            var offset = 0;
            for (var i = 0; i < columnIndex; i++)
            {
                if ((this.Spec.GetColumn(i).Attr & ColumnAttr.Indexed) != 0)
                    offset++;
            }

            return columnIndex + offset;
        }

        private BpTree GetColumnBpTree(int index)
        {
            var bpTreeRef = (ulong)this.Columns[this.GetColumnBpTreeIndex(index)];
            return new BpTree(this.Ref.NewRef(bpTreeRef));
        }

        // 1つ目のカラムから件数を取ってくる
        // https://github.com/realm/realm-core/blob/v5.12.2/src/realm/table.cpp#L6346-L6354
        public int RowCount => this.Spec.ColumnCount > 0 ? this.GetColumnBpTree(0).Count : 0;

        private (ReferenceAccessor leaf, int indexInLeaf) GetFromBpTree(int columnIndex, int rowIndex)
        {
            return this.GetColumnBpTree(columnIndex).Get(rowIndex);
        }

        private void CheckColumn(int columnIndex, ColumnType columnType, bool nullable)
        {
            // 本家の get_int とかでは、 nullable に関わらず勝手にうまくやってくれるが、
            // ここでは面倒なので完全一致でチェックする

            var spec = this.Spec.GetColumn(columnIndex);

            if (spec.Type != columnType)
                throw new InvalidOperationException($"このカラムの型は {spec.Type} です。");

            var actualNullable = (spec.Attr & ColumnAttr.Nullable) != 0;

            if (actualNullable != nullable)
                throw new InvalidOperationException(actualNullable ? "このカラムは nullable です。" : "このカラムは nullable ではありません。");
        }

        public long GetInt(int columnIndex, int rowIndex)
        {
            this.CheckColumn(columnIndex, ColumnType.Int, false);

            var (leaf, indexInLeaf) = this.GetFromBpTree(columnIndex, rowIndex);
            return new RealmArray(leaf)[indexInLeaf];
        }

        public long? GetNullableInt(int columnIndex, int rowIndex)
        {
            this.CheckColumn(columnIndex, ColumnType.Int, true);

            var (leaf, indexInLeaf) = this.GetFromBpTree(columnIndex, rowIndex);
            return new RealmArrayIntNull(leaf)[indexInLeaf];
        }

        public bool GetBool(int columnIndex, int rowIndex)
        {
            this.CheckColumn(columnIndex, ColumnType.Bool, false);

            var (leaf, indexInLeaf) = this.GetFromBpTree(columnIndex, rowIndex);
            return new RealmArray(leaf)[indexInLeaf] != 0;
        }

        public bool? GetNullableBool(int columnIndex, int rowIndex)
        {
            this.CheckColumn(columnIndex, ColumnType.Bool, true);

            var (leaf, indexInLeaf) = this.GetFromBpTree(columnIndex, rowIndex);
            return new RealmArrayIntNull(leaf)[indexInLeaf] is long v
                ? v != 0
                : (bool?)null;
        }

        public DateTimeOffset GetOldDateTime(int columnIndex, int rowIndex)
        {
            this.CheckColumn(columnIndex, ColumnType.OldDateTime, false);

            var (leaf, indexInLeaf) = this.GetFromBpTree(columnIndex, rowIndex);
            var seconds = new RealmArray(leaf)[indexInLeaf];

            // 1970/01/01 00:00:00 UTC からの秒数で保存されている
            return DateTimeOffset.FromUnixTimeSeconds(seconds);
        }

        public DateTimeOffset? GetNullableOldDateTime(int columnIndex, int rowIndex)
        {
            this.CheckColumn(columnIndex, ColumnType.OldDateTime, true);

            var (leaf, indexInLeaf) = this.GetFromBpTree(columnIndex, rowIndex);
            return new RealmArrayIntNull(leaf)[indexInLeaf] is long seconds
                ? DateTimeOffset.FromUnixTimeSeconds(seconds)
                : (DateTimeOffset?)null;
        }

        public float GetFloat(int columnIndex, int rowIndex)
        {
            this.CheckColumn(columnIndex, ColumnType.Float, false);

            var (leaf, indexInLeaf) = this.GetFromBpTree(columnIndex, rowIndex);
            var value = new RealmBasicArray<float>(leaf)[indexInLeaf];
            return IsNullFloat(value) ? 0f : value;
        }

        public float? GetNullableFloat(int columnIndex, int rowIndex)
        {
            this.CheckColumn(columnIndex, ColumnType.Float, true);

            var (leaf, indexInLeaf) = this.GetFromBpTree(columnIndex, rowIndex);
            var value = new RealmBasicArray<float>(leaf)[indexInLeaf];
            return IsNullFloat(value) ? (float?)null : value;
        }

        private static bool IsNullFloat(float value)
        {
            // NaN 領域で null を表現
            // https://github.com/realm/realm-core/blob/v5.12.2/src/realm/null.hpp#L124
            unsafe
            {
                uint v = *(uint*)&value;
                return v == 0x7fc000aa;
            }
        }

        public double GetDouble(int columnIndex, int rowIndex)
        {
            this.CheckColumn(columnIndex, ColumnType.Double, false);

            var (leaf, indexInLeaf) = this.GetFromBpTree(columnIndex, rowIndex);
            var value = new RealmBasicArray<double>(leaf)[indexInLeaf];
            return IsNullDouble(value) ? 0f : value;
        }

        public double? GetNullableDouble(int columnIndex, int rowIndex)
        {
            this.CheckColumn(columnIndex, ColumnType.Double, true);

            var (leaf, indexInLeaf) = this.GetFromBpTree(columnIndex, rowIndex);
            var value = new RealmBasicArray<double>(leaf)[indexInLeaf];
            return IsNullDouble(value) ? (double?)null : value;
        }

        private static bool IsNullDouble(double value)
        {
            // https://github.com/realm/realm-core/blob/v5.12.2/src/realm/null.hpp#L123
            unsafe
            {
                ulong v = *(ulong*)&value;
                return v == 0x7ff80000000000aa;
            }
        }

        // TODO: 続き https://github.com/realm/realm-core/blob/v5.12.2/src/realm/table.hpp#L436-L446
    }
}
