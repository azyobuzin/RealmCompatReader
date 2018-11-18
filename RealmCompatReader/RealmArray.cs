using System;
using System.IO;

namespace RealmCompatReader
{
    public class RealmArray
    {
        private readonly UnmanagedMemoryAccessor _accessor;
        private readonly ulong _ref;

        // ヘッダー

        public bool IsInnerBptreeNode
        {
            get => (this.ReadHeaderByte(4) & 0x80) != 0;
            set => throw new NotImplementedException();
        }

        /// <summary>
        /// 要素が配列かどうか
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="HasRefs"/> が <c>true</c> のとき、この配列を削除すると子の配列も削除される。
        /// この配列のクローンを作成するときは、子の配列もクローンが作成される。
        /// </para>
        /// <para>
        /// 要素が配列への参照かどうかは、「0 でないかつ最下位ビットが 0 である」で判断される。
        /// <seealso href="https://github.com/realm/realm-core/blob/v5.12.1/src/realm/array.cpp#L1605-L1608"/>
        /// </para>
        /// </remarks>
        public bool HasRefs
        {
            get => (this.ReadHeaderByte(4) & 0x40) != 0;
            set => throw new NotImplementedException();
        }

        // TODO: 調査
        // 雑な名前をやめろ
        public bool ContextFlag
        {
            get => (this.ReadHeaderByte(4) & 0x20) != 0;
            set => throw new NotImplementedException();
        }

        /// <summary>
        /// どのように配列のバイト数を計算するか
        /// </summary>
        public RealmArrayWidthType WidthType
        {
            get => (RealmArrayWidthType)((this.ReadHeaderByte(4) & 0x18) >> 3);
            set => throw new NotImplementedException();
        }

        /// <summary>
        /// 配列要素の最大ビット長
        /// </summary>
        public byte Width
        {
            get => (byte)(1 << (this.ReadHeaderByte(4) & 0x07) >> 1);
            set => throw new NotImplementedException();
        }

        /// <summary>
        /// 格納されている要素数
        /// </summary>
        public ulong Size
        {
            get => ((ulong)this.ReadHeaderByte(5) << 16) + ((ulong)this.ReadHeaderByte(6) << 8) + this.ReadHeaderByte(7);
            set => throw new NotImplementedException();
        }

        /// <summary>
        /// 格納できる要素数
        /// </summary>
        /// <remarks>
        /// 下位3ビットは捨てられる
        /// </remarks>
        public ulong Capacity
        {
            get => ((ulong)this.ReadHeaderByte(0) << 19) + ((ulong)this.ReadHeaderByte(1) << 11) + ((ulong)this.ReadHeaderByte(2) << 3);
            set => throw new NotImplementedException();
        }

        public RealmArray(UnmanagedMemoryAccessor accessor, ulong @ref)
        {
            this._accessor = accessor;
            this._ref = @ref;
        }

        public long this[ulong index]
        {
            get
            {
                if (index >= this.Size)
                    throw new ArgumentOutOfRangeException(nameof(index));

                // https://github.com/realm/realm-core/blob/v5.12.1/src/realm/array.hpp#L2003-L2046
                ulong offset;

                switch (this.Width)
                {
                    case 0:
                        return 0;
                    case 1:
                        offset = index >> 3;
                        return (this.ReadData(offset) >> (int)(index & 7)) & 0x01;
                    case 2:
                        offset = index >> 2;
                        return (this.ReadData(offset) >> ((int)(index & 3) << 1)) & 0x03;
                    case 4:
                        offset = index >> 1;
                        return (this.ReadData(offset) >> ((int)(index & 1) << 2)) & 0x0F;
                    case 8:
                        return this.ReadData(index);
                    case 16:
                        offset = index * 2;
                        return this.ReadData(offset);
                    case 32:
                        offset = index * 4;
                        return this.ReadData(offset);
                    case 64:
                        offset = index * 8;
                        return this.ReadData(offset);
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        private byte ReadHeaderByte(int offset)
        {
            return this._accessor.ReadByte(checked((long)this._ref + offset));
        }

        private long ReadData(ulong offset)
        {
            const int headerSize = 8;
            return this._accessor.ReadInt64(checked(headerSize + (long)offset));
        }
    }

    /// <summary>
    /// どのように配列のバイト数を計算するか
    /// </summary>
    // https://github.com/realm/realm-core/blob/v5.12.1/src/realm/array.hpp#L1707-L1710
    public enum RealmArrayWidthType
    {
        /// <summary>
        /// <c>(width/8) * size</c>
        /// </summary>
        Bits = 0,
        /// <summary>
        /// <c>width * size</c>
        /// </summary>
        Multiply = 1,
        /// <summary>
        /// <c>1 * size</c>
        /// </summary>
        Ignore = 2,
    }
}
