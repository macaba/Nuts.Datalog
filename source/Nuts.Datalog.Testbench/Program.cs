using Nuts.Datalog;
using System;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.Converters;
using YamlDotNet.Serialization.NamingConventions;

DatalogConfigurationFile configurationFile2 = new()
{
    ApiFields = new()
    {
        new()
        {
            Field = "timestamp", 
            Type = DatalogFieldType.Timestamp 
        },
        new()
        {
            Field = "reading", 
            Type = DatalogFieldType.Double
        }
    },
    CodeFields = new()
    {
        new()
        {
            Field = "codeInt32",
            Type = DatalogFieldType.Int32,
            Code = "1 + 2"
        },
        new()
        {
            Field = "codeString",
            Type = DatalogFieldType.String,
            Code = "\"test\" + \"2\""
        },
        new()
        {
            Field = "codeMultiline",
            Type = DatalogFieldType.String,
            Code =
@"var test = Math.Sqrt(2);
var something = test + 2.0;
something.ToString() + ""2"""
        }
    },
    Columns = new()
    {
        new DatalogConfigColumn()
        {
            Field = "ServerTimestamp",
            Header = "Server timestamp",
            Format = "o"
        },
        new DatalogConfigColumn()
        {
            Field = "ServerTimestamp",
            Header = "Server timestamp (Excel)",
            Format = "yyyy-MM-dd HH:mm:ss"
         },
        //new DatalogConfigColumn()
        //{
        //    Field = "ServerTimestamp",
        //    Header = "Server seconds since creation",
        //    Format = "F3"
        // },
        new DatalogConfigColumn()
        {
            Field = "timestamp",
            Header = "Reading timestamp",
            Format = "o"
         },
        new DatalogConfigColumn()
        {
            Field = "reading",
            Header = "3458A reading",
            Format = "F7"
         }
    }
};


var serializer = new SerializerBuilder()
    .WithNamingConvention(CamelCaseNamingConvention.Instance)
    .WithTypeConverter(new DateTimeOffsetConverter())
    .Build();
var yaml = serializer.Serialize(configurationFile2);
File.WriteAllText("HelloWorld.config", yaml);