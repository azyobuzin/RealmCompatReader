using System;

namespace RealmCompatReader
{
    public class TableSpec
    {
        public ReferenceAccessor Ref { get; }

        // Spec の子配列たち: https://github.com/realm/realm-core/blob/v5.12.1/src/realm/spec.hpp#L132-L136
        private readonly RealmArray _types;
        private readonly RealmArrayString _names;
        private readonly RealmArray _attr;
        private readonly RealmArray _subspecs; // optional
        // TODO: enumkeys (optional)

        // カラム数は types から取得するのが正解
        // https://github.com/realm/realm-core/blob/v5.12.1/src/realm/spec.hpp#L280-L284
        public int ColumnCount => this._types.Count;

        public TableSpec(ReferenceAccessor @ref)
        {
            this.Ref = @ref;

            var specArray = new RealmArray(@ref);
            this._types = new RealmArray(@ref.NewRef((ulong)specArray[0]));
            this._names = new RealmArrayString(@ref.NewRef((ulong)specArray[1]), false);
            this._attr = new RealmArray(@ref.NewRef((ulong)specArray[2]));

            if (specArray.Count >= 4)
            {
                var subspecsRef = (ulong)specArray[3];
                if (subspecsRef != 0)
                    this._subspecs = new RealmArray(@ref.NewRef(subspecsRef));
            }
        }

        public ColumnSpec GetColumn(int index)
        {
            if ((uint)index >= (uint)this.ColumnCount)
                throw new ArgumentOutOfRangeException(nameof(index));

            return new ColumnSpec(
                (ColumnType)this._types[index],
                // バックリンクは一番後ろにあり、名前を持たないので names の要素として存在しない
                index < this._names.Count ? this._names[index] : null,
                (ColumnAttr)this._attr[index]
            );
        }

        /// <summary>
        /// <see cref="ColumnType.Link"/>、 <see cref="ColumnType.LinkList"/>、 <see cref="ColumnType.BackLink"/> のリンク先テーブルを取得する。
        /// </summary>
        public int GetLinkTargetTableIndex(int columnIndex)
        {
            switch (this.GetColumn(columnIndex).Type)
            {
                case ColumnType.Link:
                case ColumnType.LinkList:
                case ColumnType.BackLink:
                    break;
                default:
                    throw new InvalidOperationException();
            }

            var subspecIndex = this.GetSubspecIndex(columnIndex);
            var taggedValue = this._subspecs[subspecIndex];

            // subspecs は HasRef 配列なので、参照でないものは最下位ビットに細工がある
            var targetIndex = (ulong)taggedValue >> 1;
            return checked((int)targetIndex);
        }

        /// <summary>
        /// <see cref="ColumnType.BackLink"/> のリンク先カラムを取得する。
        /// </summary>
        public int GetBacklinkOriginColumnIndex(int columnIndex)
        {
            if (this.GetColumn(columnIndex).Type != ColumnType.BackLink)
                throw new InvalidOperationException();

            var subspecIndex = this.GetSubspecIndex(columnIndex);
            var taggedValue = this._subspecs[subspecIndex + 1];

            var targetIndex = (ulong)taggedValue >> 1;
            return checked((int)targetIndex);
        }

        /// <summary>
        /// <see cref="ColumnType.Table"/> のテーブル定義を取得する。
        /// </summary>
        public TableSpec GetSubspec(int columnIndex)
        {
            if (this.GetColumn(columnIndex).Type != ColumnType.Table)
                throw new InvalidOperationException();

            var subspecIndex = this.GetSubspecIndex(columnIndex);
            var subspecRef = (ulong)this._subspecs[subspecIndex];
            return new TableSpec(this.Ref.NewRef(subspecRef));
        }

        private int GetSubspecIndex(int columnIndex)
        {
            // subspec はカラムの型によって必要な要素数が違うので、
            // columnIndex 番目のカラム用の subspec のインデックスを計算する必要がある
            // https://github.com/realm/realm-core/blob/v5.12.7/src/realm/spec.cpp#L332-L369

            var subspecIndex = 0;

            for (var i = 0; i < columnIndex; i++)
            {
                switch (this.GetColumn(i).Type)
                {
                    case ColumnType.Table:
                    case ColumnType.Link:
                    case ColumnType.LinkList:
                        subspecIndex += 1;
                        break;
                    case ColumnType.BackLink:
                        subspecIndex += 2;
                        break;
                }
            }

            return subspecIndex;
        }
    }
}
