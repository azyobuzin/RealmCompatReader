using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;

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
                var topArray = new RealmArray(accessor, header.GetTopRef().Ref);

                Console.WriteLine("Top Array Count: {0}", topArray.Header.Size);

                // HasRefs な Array に数値が入っているので、最下位ビットが 1 になっている
                // ビットシフトで元の値が手に入る
                Console.WriteLine("Logical File Size: {0}", (ulong)topArray[2] >> 1);

                // 配列の 1 番目がテーブル名リスト
                var tableNameArray = new RealmArrayString(accessor, (ulong)topArray[0], false);

                // 2 番目がテーブルのリスト
                var tableArray = new RealmArray(accessor, (ulong)topArray[1]);

                Console.WriteLine("Tables");
                for (var i = 0; i < tableNameArray.Header.Size; i++)
                {
                    Console.WriteLine(" - " + tableNameArray[i]);

                    var spec = new RealmTable(accessor, (ulong)tableArray[i]).Spec;
                    for (var j = 0; j < spec.ColumnCount; j++)
                    {
                        var column = spec.GetColumn(j);
                        Console.WriteLine("    - {0}: {1} ({2})", column.Name ?? "(Backlink Column)", column.Type, column.Attr);
                    }
                }

                Debugger.Break();
            }
        }
    }
}
