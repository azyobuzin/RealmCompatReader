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

                Console.WriteLine("Tables");
                for (var i = 0; i < tableNameArray.Count; i++)
                {
                    var table = new RealmTable(new ReferenceAccessor(accessor, (ulong)tableArray[i]));

                    Console.WriteLine(" - {0} (Count: {1})", tableNameArray[i], table.RowCount);

                    var spec = table.Spec;
                    for (var j = 0; j < spec.ColumnCount; j++)
                    {
                        var column = spec.GetColumn(j);
                        Console.WriteLine("    - {0}: {1} ({2})", column.Name ?? "(Backlink Column)", column.Type, column.Attr);
                    }
                }

                // CurrencyRate テーブルの中身を見てみる
                /*
                Console.WriteLine("CurrencyRateIDs");
                var currencyRateTableIndex = Enumerable.Range(0, tableNameArray.Count)
                    .FirstOrDefault(i => tableNameArray[i] == "class_CurrencyRate");
                var bpTree = new RealmTable(new ReferenceAccessor(accessor, (ulong)tableArray[currencyRateTableIndex]))
                    .GetColumnBpTree(0);
                for (var i = 0; i < bpTree.Count; i++)
                {
                    var (leafRef, indexInLeaf) = bpTree.Get(i);
                    var leaf = new RealmArray(leafRef);
                    Console.WriteLine(" - {0}", leaf[indexInLeaf]);
                }
                */

                Debugger.Break();
            }
        }
    }
}
