namespace ChatbotMVC.Data.Schema
{
    public class SchemaTable
    {
        public string Name { get; set; }
        public int RowCount { get; set; }
        public List<SchemaColumn> Columns { get; set; } = new List<SchemaColumn>();
    }

    public class SchemaColumn
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public bool Nullable { get; set; }
    }
}
