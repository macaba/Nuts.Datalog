using YamlDotNet.Serialization;
using YamlDotNet.Serialization.Converters;
using YamlDotNet.Serialization.NamingConventions;

namespace Nuts.Datalog
{
    // The datalog.config and datalog.csv will have the same filename (different extension) and will be in the same directory
    public class DatalogConfigurationFile
    {
        public List<DatalogConfigApiField> ApiFields { get; set; }
        public List<DatalogConfigCodeField> CodeFields { get; set; }
        public List<DatalogConfigColumn> Columns { get; set; }
        public DatalogStorageFormat StorageFormat { get; set; }

        public void WriteToDisk(string path)
        {
            //var options = new JsonSerializerOptions { WriteIndented = true, ReadCommentHandling = JsonCommentHandling.Skip };
            //var converter = new JsonStringEnumConverter();
            //options.Converters.Add(converter);
            //var json = JsonSerializer.Serialize(this, options);
            //File.WriteAllText(path, json);

            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .WithTypeConverter(new DateTimeOffsetConverter())
                .Build();
            var yaml = serializer.Serialize(this);
            File.WriteAllText(path, yaml);
        }

        public static DatalogConfigurationFile ReadFromDisk(string path)
        {
            //var options = new JsonSerializerOptions { WriteIndented = true, ReadCommentHandling = JsonCommentHandling.Skip };
            //var converter = new JsonStringEnumConverter();
            //options.Converters.Add(converter);
            //var json = File.ReadAllText(path);
            //return JsonSerializer.Deserialize<DatalogConfigurationFile>(json, options);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .WithTypeConverter(new DateTimeOffsetConverter())
                .Build();
            var yaml = File.ReadAllText(path);
            return deserializer.Deserialize<DatalogConfigurationFile>(yaml);
        }
    }

    public class DatalogConfigApiField
    {
        public required string Field { get; set; }
        public required DatalogFieldType Type { get; set; }
    }

    public class DatalogConfigCodeField
    {
        public required string Field { get; set; }
        public required DatalogFieldType Type { get; set; }
        public required string Code { get; set; }
    }

    public class DatalogConfigColumn
    {
        public required string Field { get; set; }
        public required string Header { get; set; }
        public required string Format { get; set; }
    }

    public class DatalogStorageFormat
    {
        public required DatalogFileFormatDelimiter Delimiter { get; set; }
        public required string Extension { get; set; }
    }

    public enum DatalogFieldType
    {
        String,
        Int32,
        Float,
        Double,
        Timestamp
    }

    public enum DatalogFileFormatDelimiter
    {
        Comma,
        Semicolon,
        Tab
    }
}
