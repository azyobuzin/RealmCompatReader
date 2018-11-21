using System.IO;

namespace RealmCompatReader
{
    public class ReferenceAccessor
    {
        public UnmanagedMemoryAccessor Accessor { get; }
        public ulong Ref { get; }

        public ReferenceAccessor(UnmanagedMemoryAccessor accessor, ulong @ref)
        {
            this.Accessor = accessor;
            this.Ref = @ref;
        }

        public byte ReadByte(long offset)
        {
            return this.Accessor.ReadByte(this.Pos(offset));
        }

        public sbyte ReadSByte(long offset)
        {
            return this.Accessor.ReadSByte(this.Pos(offset));
        }

        public short ReadInt16(long offset)
        {
            return this.Accessor.ReadInt16(this.Pos(offset));
        }

        public int ReadInt32(long offset)
        {
            return this.Accessor.ReadInt32(this.Pos(offset));
        }

        public long ReadInt64(long offset)
        {
            return this.Accessor.ReadInt64(this.Pos(offset));
        }

        public void ReadBytes(long offset, byte[] array, int destOffset, int count)
        {
            var result = this.Accessor.ReadArray(this.Pos(offset), array, destOffset, count);
            if (result != count) throw new InvalidDataException();
        }

        public T Read<T>(long offset) where T : struct
        {
            this.Accessor.Read(this.Pos(offset), out T value);
            return value;
        }

        public ReferenceAccessor NewRef(ulong @ref)
        {
            return new ReferenceAccessor(this.Accessor, @ref);
        }

        private long Pos(long offset)
        {
            return checked((long)this.Ref + offset);
        }
    }
}
