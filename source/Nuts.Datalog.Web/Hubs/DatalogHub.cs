using Microsoft.AspNetCore.SignalR;

namespace Nuts.Datalog.Web
{
    public class DatalogHub : Hub
    {
        private readonly string configDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Configs");
        private readonly string datalogDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Datalogs");

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
                    Columns = columns
                };

                await Clients.Caller.SendAsync("Data", response);
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
