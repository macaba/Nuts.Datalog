//using CsvHelper;
//using CsvHelper.Configuration;
using Microsoft.AspNetCore.SignalR;
using NReco.Csv;
using System.Globalization;
using System.Text.Json;

namespace Nuts.Datalog.Web
{
    public class DatalogHub : Hub
    {
        string configDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Configs");
        string datalogDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Datalogs");

        public async Task Enumerate()
        {
            var paths = Directory.EnumerateFiles(configDirectory);
            var datalogNames = paths.Select(path => Path.GetFileNameWithoutExtension(path));
            await Clients.Caller.SendAsync("Enumerate", datalogNames);
        }

        public async Task Data(string datalog)
        {
            var configPath = Path.Combine(configDirectory, datalog + ".config");
            var datalogPath = Path.Combine(datalogDirectory, datalog + ".csv");

            if (File.Exists(configPath) && File.Exists(datalogPath))
            {
                // Now load the config, get the column names, map it to ordinal column in the CSV and build up an array of objects
                var config = DatalogConfigurationFile.ReadFromDisk(configPath);

                var columns = new List<DatalogColumn>();

                foreach(var column in config.Columns)
                {
                    if(config.ApiFields.Any(apiField => apiField.Field == column.Field))
                    {
                        columns.Add(new DatalogColumn() { Field = column.Field, Type = config.ApiFields.First(apiField => apiField.Field == column.Field).Type, Header = column.Header, Format = column.Format });
                    }
                    else if(config.CodeFields.Any(codeField => codeField.Field == column.Field))
                    {
                        columns.Add(new DatalogColumn() { Field = column.Field, Type = config.CodeFields.First(codeField => codeField.Field == column.Field).Type, Header = column.Header, Format = column.Format });
                    }                    
                }

                var response = new DataResponseDto
                {
                    Columns = columns,
                    Data = new()
                };

                // This approach is slower than it could be because everything is being converted into UTF16 strings. It might be better to use a CSV reader that can read the data as it is in the file and return ReadOnlySpan<byte>?
                var csvHeaders = new List<string>();
                using (var stream = new FileStream(datalogPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan))
                using (var streamRdr = new StreamReader(stream))
                {
                    var csvReader = new CsvReader(streamRdr, ",");
                    csvReader.Read();   // Read header
                    for(int i = 0; i < csvReader.FieldsCount; i++)
                    {
                        csvHeaders.Add(csvReader[i]);
                    }
                    while (csvReader.Read())
                    {
                        var fields = new string[csvReader.FieldsCount];
                        for (int i = 0; i < csvReader.FieldsCount; i++)
                        {
                            fields[i] = csvReader[i];
                        }
                        response.Data.Add(fields);
                    }
                }

                await Clients.Caller.SendAsync("Data", response);
                //foreach (var column in config.Columns)
                //{

                //}

                //var csvRecords = ReadCsvFile(datalogPath);

                //if (csvRecords != null && csvRecords.Any())
                //{
                //    string jsonData = ConvertCsvToJSON(csvRecords);
                //    await Clients.Caller.SendAsync("Data", jsonData);
                //}




                //var lines = File.ReadAllLines(datalogPath);
                //var data = lines.Select(line => line.Split(',').Select(value => double.Parse(value)));
                //await Clients.Caller.SendAsync("Data", data);
            }
        }

        //static string ConvertCsvToJSON(List<dynamic> csvRecords)
        //{
        //    return JsonSerializer.Serialize(csvRecords);
        //}

        //public async Task SendMessage(string user, string message)
        //{
        //    await Clients.All.SendAsync("ReceiveMessage", user, message);
        //}
    }
}
