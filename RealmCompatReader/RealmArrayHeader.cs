using System;

namespace RealmCompatReader
{
    public class RealmArrayHeader
    {
        /// <summary>
        /// ヘッダーのバイト数
        /// </summary>
        public const int HeaderSize = 8;

        public ReferenceAccessor Ref { get; }

        public RealmArrayHeader(ReferenceAccessor @ref)
        {
            this.Ref = @ref;
        }

        // ヘッダーの構造: https://github.com/realm/realm-core/blob/v5.12.1/src/realm/array.cpp#L46-L88

        public bool IsInnerBptreeNode
        {
            get => (this.ReadByte(4) & 0x80) != 0;
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
            get => (this.ReadByte(4) & 0x40) != 0;
            set => throw new NotImplementedException();
        }

        // TODO: 調査
        // 雑な名前をやめろ
        public bool ContextFlag
        {
            get => (this.ReadByte(4) & 0x20) != 0;
            set => throw new NotImplementedException();
        }

        /// <summary>
        /// どのように配列のバイト数を計算するか
        /// </summary>
        public RealmArrayWidthType WidthType
        {
            get => (RealmArrayWidthType)((this.ReadByte(4) & 0x18) >> 3);
            set => throw new NotImplementedException();
        }

        /// <summary>
        /// 配列要素の最大ビット長
        /// </summary>
        public byte Width
        {
            get => (byte)(1 << (this.ReadByte(4) & 0x07) >> 1);
            set => throw new NotImplementedException();
        }

        /// <summary>
        /// 格納されている要素数
        /// </summary>
        public int Size
        {
            get => (int)(((uint)this.ReadByte(5) << 16) + ((uint)this.ReadByte(6) << 8) + this.ReadByte(7));
            set => throw new NotImplementedException();
        }

        /// <summary>
        /// 格納できる要素数
        /// </summary>
        /// <remarks>
        /// 下位3ビットは捨てられる。
        /// ファイルに書き込まれているデータの場合は、この値は関係ない。
        /// </remarks>
        public int Capacity
        {
            get => (int)(((uint)this.ReadByte(0) << 19) + ((uint)this.ReadByte(1) << 11) + ((uint)this.ReadByte(2) << 3));
            set => throw new NotImplementedException();
        }

        private byte ReadByte(int offset)
        {
            return this.Ref.ReadByte(offset);
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
