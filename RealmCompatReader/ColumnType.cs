using System;

namespace RealmCompatReader
{
    // https://github.com/realm/realm-core/blob/v5.12.1/src/realm/column_type.hpp

    public enum ColumnType
    {
        Int = 0,
        Bool = 1,
        String = 2,
        StringEnum = 3, // double refs
        Binary = 4,
        Table = 5,
        Mixed = 6,
        OldDateTime = 7,
        Timestamp = 8,
        Float = 9,
        Double = 10,
        Reserved4 = 11, // Decimal
        Link = 12,
        LinkList = 13,
        BackLink = 14,
    }

    [Flags]
    public enum ColumnAttr
    {
        None = 0,
        Indexed = 1,
        /// <remarks><see cref="Indexed"/> と一緒に指定する</remarks>
        Unique = 2,
        Reserved = 4,
        // TODO: Strong と Weak の違い
        StrongLinks = 8,
        Nullable = 16,
    }
}
