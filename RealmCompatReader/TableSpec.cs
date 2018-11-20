﻿using System;

namespace RealmCompatReader
{
    public class TableSpec
    {
        public ReferenceAccessor Ref { get; }

        // Spec の子配列たち: https://github.com/realm/realm-core/blob/v5.12.1/src/realm/spec.hpp#L132-L136
        private readonly RealmArray _types;
        private readonly RealmArrayString _names;
        private readonly RealmArray _attr;
        // TODO: optional なもの (subspecs, enumkeys)

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
        }

        public ColumnSpec GetColumn(int index)
        {
            if ((uint)index >= this.ColumnCount)
                throw new ArgumentOutOfRangeException(nameof(index));

            return new ColumnSpec(
                (ColumnType)this._types[index],
                // バックリンクは一番後ろにあり、名前を持たないので names の要素として存在しない
                index < this._names.Count ? this._names[index] : null,
                (ColumnAttr)this._attr[index]
            );
        }
    }
}
