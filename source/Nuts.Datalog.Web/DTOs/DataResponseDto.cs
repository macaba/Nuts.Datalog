namespace Nuts.Datalog.Web
{
    public class DatalogColumn
    {
        public required string Field { get; set; }
        public required DatalogFieldType Type { get; set; }
        public required string Header { get; set; }
        public required string Format { get; set; }
    }

    public class DataResponseDto
    {
        public List<DatalogColumn> Columns { get; set; }
        public List<string[]> Data { get; set; }
    }
}
