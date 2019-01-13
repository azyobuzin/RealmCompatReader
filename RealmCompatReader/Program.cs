using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;

namespace RealmCompatReader
{
    class Program
    {
        static void Main(string[] args)
        {
            var fileName = args.Length > 0 ? args[0] : "default.realm";

            using (var mmf = MemoryMappedFile.CreateFromFile(fileName, FileMode.Open, null, 0, MemoryMappedFileAccess.Read))
            using (var accessor = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read))
            {
                var header = RealmFileHeader.ReadFrom(accessor);

                Console.WriteLine("File Format Version: {0}", header.GetTopRef().FileFormatVersion);

                // ファイル内では、 ref の値はそのまま使える（追加でアロケーションすると話は別）
                var topArray = new RealmArray(new ReferenceAccessor(accessor, header.GetTopRef().Ref));

                Console.WriteLine("Top Array Count: {0}", topArray.Count);

                // HasRefs な Array に数値が入っているので、最下位ビットが 1 になっている
                // ビットシフトで元の値が手に入る
                Console.WriteLine("Logical File Size: {0}", (ulong)topArray[2] >> 1);

                // 配列の 1 番目がテーブル名リスト
                var tableNameArray = new RealmArrayString(new ReferenceAccessor(accessor, (ulong)topArray[0]), false);

                // 2 番目がテーブルのリスト
                var tableArray = new RealmArray(new ReferenceAccessor(accessor, (ulong)topArray[1]));

                Console.WriteLine();
                Console.WriteLine("Tables");
                for (var i = 0; i < tableNameArray.Count; i++)
                {
                    var table = new RealmTable(new ReferenceAccessor(accessor, (ulong)tableArray[i]));

                    Console.WriteLine(" - {0} (Count: {1})", tableNameArray[i], table.RowCount);

                    var spec = table.Spec;
                    for (var j = 0; j < spec.ColumnCount; j++)
                    {
                        var column = spec.GetColumn(j);
                        var columnName = column.Name;
                        var columnType = column.Type;

                        Console.Write("    - ");

                        if (columnName != null)
                            Console.Write("{0}: ", columnName);

                        Console.Write(columnType);

                        switch (columnType)
                        {
                            case ColumnType.Link:
                            case ColumnType.LinkList:
                                {
                                    var targetTableIndex = spec.GetLinkTargetTableIndex(j);
                                    Console.Write(" -> {0}", tableNameArray[targetTableIndex]);
                                }
                                break;
                            case ColumnType.BackLink:
                                {
                                    var targetTableIndex = spec.GetLinkTargetTableIndex(j);
                                    var targetColumnIndex = spec.GetBacklinkOriginColumnIndex(j);
                                    Console.Write(" <- ");
                                    Console.Write(GetBacklinkTarget(targetTableIndex, targetColumnIndex));
                                }
                                break;
                        }

                        Console.WriteLine(" ({0})", column.Attr);
                    }
                }

                Console.WriteLine();
                Console.WriteLine("Contents");
                for (var i = 0; i < tableNameArray.Count; i++)
                {
                    var table = new RealmTable(new ReferenceAccessor(accessor, (ulong)tableArray[i]));
                    var count = Math.Min(table.RowCount, 10); // 10件まで
                    Console.WriteLine(" - {0} ({1}/{2})", tableNameArray[i], count, table.RowCount);
                    PrintTableContent(table, GetBacklinkTarget, count);
                }

                string GetBacklinkTarget(int tableIndex, int columnIndex)
                {
                    var targetTable = new RealmTable(tableArray.Ref.NewRef((ulong)tableArray[tableIndex]));
                    var targetColumnName = targetTable.Spec.GetColumn(columnIndex).Name;
                    return tableNameArray[tableIndex] + "." + targetColumnName;
                }

                Debugger.Break();
            }
        }

        private static void PrintTableContent(RealmTable table, Func<int, int, string> getBacklinkTarget, int count)
        {
            var spec = table.Spec;

            for (var rowIndex = 0; rowIndex < count; rowIndex++)
            {
                for (var columnIndex = 0; columnIndex < spec.ColumnCount; columnIndex++)
                {
                    var columnSpec = spec.GetColumn(columnIndex);
                    var columnName = columnSpec.Name;

                    object value;
                    switch (columnSpec.Type)
                    {
                        case ColumnType.Int:
                            value = columnSpec.Nullable
                                ? table.GetNullableInt(columnIndex, rowIndex)
                                : table.GetInt(columnIndex, rowIndex);
                            break;
                        case ColumnType.Bool:
                            value = columnSpec.Nullable
                                ? table.GetNullableBool(columnIndex, rowIndex)
                                : table.GetBool(columnIndex, rowIndex);
                            break;
                        case ColumnType.String:
                            value = table.GetString(columnIndex, rowIndex);
                            break;
                        case ColumnType.Binary:
                            var bin = table.GetBinary(columnIndex, rowIndex);
                            if (bin == null)
                            {
                                value = null;
                            }
                            else
                            {
                                value = string.Concat(bin.Select(x => x.ToString("x2")).Prepend("0x"));
                            }
                            break;
                        case ColumnType.OldDateTime:
                            value = columnSpec.Nullable
                                ? table.GetNullableOldDateTime(columnIndex, rowIndex)
                                : table.GetOldDateTime(columnIndex, rowIndex);
                            break;
                        case ColumnType.Timestamp:
                            value = table.GetTimestamp(columnIndex, rowIndex);
                            break;
                        case ColumnType.Float:
                            value = columnSpec.Nullable
                                ? table.GetNullableFloat(columnIndex, rowIndex)
                                : table.GetFloat(columnIndex, rowIndex);
                            break;
                        case ColumnType.Double:
                            value = columnSpec.Nullable
                                ? table.GetNullableDouble(columnIndex, rowIndex)
                                : table.GetDouble(columnIndex, rowIndex);
                            break;
                        case ColumnType.Link:
                            value = table.GetLink(columnIndex, rowIndex);
                            break;
                        case ColumnType.LinkList:
                            value = string.Join(", ", table.GetLinkList(columnIndex, rowIndex));
                            break;
                        case ColumnType.BackLink:
                            columnName = "<- " + getBacklinkTarget(spec.GetLinkTargetTableIndex(columnIndex), spec.GetBacklinkOriginColumnIndex(columnIndex));
                            value = string.Join(", ", table.GetBacklinks(columnIndex, rowIndex));
                            break;
                        default:
                            value = "(unsupported type)";
                            break;
                    }

                    Console.WriteLine(
                        "    {0} {1}: {2}",
                        columnIndex == 0 ? "-" : " ",
                        columnName,
                        value ?? "(null)");
                }
            }
        }
    }
}
