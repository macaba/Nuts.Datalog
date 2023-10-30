using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nuts.Datalog
{
    // The datalog.json and datalog.csv will have the same filename (different extension) and will be in the same directory
    public class DatalogJsonConfiguration
    {
        public List<DatalogJsonApiField> ApiFields { get; set; }
        public List<DatalogJsonConfigurationColumn> Columns { get; set; }
        public DatalogJsonMetadata Metadata { get; set; }

        public void WriteToDisk(string path)
        {
            var options = new JsonSerializerOptions { WriteIndented = true, ReadCommentHandling = JsonCommentHandling.Skip };
            var converter = new JsonStringEnumConverter();
            options.Converters.Add(converter);
            string jsonString = JsonSerializer.Serialize(this, options);
            File.WriteAllText(path, jsonString);
        }

        public static DatalogJsonConfiguration ReadFromDisk(string path)
        {
            var options = new JsonSerializerOptions { WriteIndented = true, ReadCommentHandling = JsonCommentHandling.Skip };
            var converter = new JsonStringEnumConverter();
            options.Converters.Add(converter);
            string jsonString = File.ReadAllText(path);
            return JsonSerializer.Deserialize<DatalogJsonConfiguration>(jsonString, options);
        }
    }

    public class DatalogJsonApiField
    {
        public required string ApiField { get; set; }
        public required DatalogType Type { get; set; }
    }

    public class DatalogJsonConfigurationColumn
    {
        public required string Name { get; set; }
        public required string StringInterpolation { get; set; }
    }

    public class DatalogJsonMetadata
    {
        public DateTimeOffset? CreationTimestampUtc { get; set; }
    }

    public class DatalogJsonCharts
    {

    }

    public enum DatalogType
    {
        String,
        Int32,
        Float,
        Double,
        Timestamp
    }

    //public enum DatalogColumnSource
    //{
    //    ApiInput,               // Data comes from input API
    //    StringInterpolation,    // Data is derived from other columns or inline code
    //}
}
