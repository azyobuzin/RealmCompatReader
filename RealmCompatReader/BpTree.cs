namespace RealmCompatReader
{
    public class BpTree
    {
        public ReferenceAccessor Ref { get; }
        private readonly RealmArray _root;

        public BpTree(ReferenceAccessor @ref)
        {
            this.Ref = @ref;
            this._root = new RealmArray(@ref);
        }

        private bool RootIsLeaf => !this._root.Header.IsInnerBptreeNode;

        public int Count
        {
            get
            {
                if (this.RootIsLeaf)
                {
                    // ルートの IsInnerBptreeNode が false ならば
                    // ルートの配列の要素数しかない
                    return this._root.Count;
                }

                // lastValue = 1 + 2*total_elems_in_tree
                var lastValue = this._root[this._root.Count - 1];
                return checked((int)lastValue / 2);
            }
        }

        public (ReferenceAccessor leaf, int indexInLeaf) Get(int index)
        {
            if (this.RootIsLeaf)
            {
                // ルートの IsInnerBptreeNode が false ならば
                // 木構造になっていないので、ルートの配列をそのまま返す
                return (this.Ref, index);
            }

            var t = (childRef: this.Ref, indexInChild: index);
            while (true)
            {
                t = this.FindChild(new RealmArray(t.childRef), t.indexInChild);

                var childIsLeaf = !new RealmArrayHeader(t.childRef).IsInnerBptreeNode;
                if (childIsLeaf)
                {
                    // 葉ノードに到達
                    return t;
                }
            }
        }

        private (ReferenceAccessor childRef, int indexInChild) FindChild(RealmArray innerNode, int index)
        {
            // https://github.com/realm/realm-core/blob/v5.12.1/src/realm/bptree.cpp#L69-L113
            // Compact Form と General Form: https://github.com/realm/realm-core/blob/v5.12.1/src/realm/array.cpp#L91-L139

            int childIndex, indexInChild;

            var firstValue = innerNode[0];

            if (firstValue % 2 != 0)
            {
                // Compact Form
                // invar:bptree-node-form により、子孫ノードはすべて Compact Form であることがわかっているので
                // elemsPerChild からインデックスを計算できる

                // firstValue = 1 + 2 * elems_per_child
                var elemsPerChild = checked((int)(firstValue / 2));

                childIndex = index / elemsPerChild;
                indexInChild = index % elemsPerChild;
            }
            else
            {
                // General Form
                // firstValue は offset 配列への参照（参照なので最下位ビットは 0 になり、この if が成立）

                // offsetArray の i 番目の要素は i+1 の子までに何個の子要素が含まれているか
                // このノードの子ノードが 1 つしかないならば、 offsetArray は 0 件
                var offsetArray = new RealmArray(this.Ref.NewRef((ulong)firstValue));

                // 何番目の子ノードに index が含まれているかを探す
                childIndex = 0;
                for (; childIndex < offsetArray.Count; childIndex++)
                {
                    if (offsetArray[childIndex] > index) break;
                }

                // childIndex - 1 までの子ノードに何個の要素が含まれているかを求める
                var elemIndexOffset = childIndex == 0
                    ? 0
                    : checked((int)offsetArray[childIndex - 1]);

                indexInChild = index - elemIndexOffset;
            }

            // innerNode の最初の要素を飛ばすので、 +1
            return (this.Ref.NewRef((ulong)innerNode[1 + childIndex]), indexInChild);
        }
    }
}
