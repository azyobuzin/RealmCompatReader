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

                Console.WriteLine("Top Array Count: {0}", topArray.Size);

                Debugger.Break();
            }
        }
    }
}
