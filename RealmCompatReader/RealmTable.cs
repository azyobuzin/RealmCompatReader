using System.IO;

namespace RealmCompatReader
{
    public class RealmTable
    {
        public TableSpec Spec { get; }
        public RealmArray Columns { get; }

        public RealmTable(UnmanagedMemoryAccessor accessor, ulong @ref)
        {
            var tableArray = new RealmArray(accessor, @ref);
            this.Spec = new TableSpec(accessor, (ulong)tableArray[0]);
            this.Columns = new RealmArray(accessor, (ulong)tableArray[1]);
        }
    }
}
