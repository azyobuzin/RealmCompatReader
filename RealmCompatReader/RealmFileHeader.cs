using System.IO;

namespace RealmCompatReader
{
    public class RealmFileHeader
    {
        public TopRef[] TopRefs { get; set; }

        public byte Flags { get; set; }

        public TopRef GetTopRef()
        {
            // Flags の最下位ビットが、どちらの ref を使うかを表す
            return this.TopRefs[this.Flags & 1];
        }

        public static RealmFileHeader ReadFrom(UnmanagedMemoryAccessor accessor)
        {
            // ヘッダーの構成
            // https://github.com/realm/realm-core/blob/v5.12.1/src/realm/alloc_slab.hpp#L462-L471

            // マジックナンバーのチェック
            if (accessor.ReadByte(16) != 'T'
                || accessor.ReadByte(17) != '-'
                || accessor.ReadByte(18) != 'D'
                || accessor.ReadByte(19) != 'B')
            {
                throw new InvalidDataException();
            }

            return new RealmFileHeader()
            {
                TopRefs = new[]
                {
                    new TopRef(accessor.ReadUInt64(0), accessor.ReadByte(20)),
                    new TopRef(accessor.ReadUInt64(8), accessor.ReadByte(21)),
                },
                Flags = accessor.ReadByte(23),
            };
        }
    }

    public struct TopRef
    {
        public ulong Ref { get; set; }
        public uint FileFormatVersion { get; set; }

        public TopRef(ulong @ref, uint fileFormatVersion)
        {
            this.Ref = @ref;
            this.FileFormatVersion = fileFormatVersion;
        }
    }
}
