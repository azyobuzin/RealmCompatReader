namespace RealmCompatReader
{
    public class ColumnSpec
    {
        public ColumnType Type { get; set; }

        public string Name { get; set; }

        public ColumnAttr Attr { get; set; }

        public ColumnSpec() { }

        public ColumnSpec(ColumnType type, string name, ColumnAttr attr)
        {
            this.Type = type;
            this.Name = name;
            this.Attr = attr;
        }
    }
}
