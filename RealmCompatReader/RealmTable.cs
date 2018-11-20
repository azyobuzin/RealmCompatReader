namespace RealmCompatReader
{
    public class RealmTable
    {
        public ReferenceAccessor Ref { get; }
        public TableSpec Spec { get; }
        public RealmArray Columns { get; }

        public RealmTable(ReferenceAccessor @ref)
        {
            this.Ref = @ref;

            var tableArray = new RealmArray(@ref);
            this.Spec = new TableSpec(@ref.NewRef((ulong)tableArray[0]));
            this.Columns = new RealmArray(@ref.NewRef((ulong)tableArray[1]));
        }

        // TODO そして注意: Indexed カラムは Columns の中で 2 要素使用する
        // https://github.com/realm/realm-core/blob/v5.12.1/src/realm/spec.cpp#L526-L538

        public BpTree GetColumnBpTree(int index)
        {
            return new BpTree(this.Ref.NewRef((ulong)this.Columns[index]));
        }
    }
}
