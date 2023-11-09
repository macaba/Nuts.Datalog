using CsvHelper;
using Microsoft.CodeAnalysis.Scripting;
using Nuts.Datalog.Scripting;
using System.Dynamic;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Nuts.Datalog
{
    public class DatalogWriter
    {
        private readonly ScriptingGlobalTypeCache globalTypeCache;
        private readonly DatalogConfigurationFile configFile;
        private readonly CsvWriter csvWriter;

        private Dictionary<string, object> scriptRunners = new();

        private HashSet<string> columnFields = new();
        private HashSet<string> firstValueInColumnHasBeenSet = new();
        private dynamic firstValueInColumnExpando = new ExpandoObject();

        public DatalogWriter(string configFilePath, string datalogDirectory)
        {
            globalTypeCache = new ScriptingGlobalTypeCache();
            configFile = DatalogConfigurationFile.ReadFromDisk(configFilePath);
            if (configFile is null)
                throw new ApplicationException($"Could not deserialize {Path.GetFileName(configFilePath)}");

            // Create a hashset of all the column fields
            foreach(var column in configFile.Columns)
            {
                columnFields.Add(column.Field);
            }

            // Create the CSV file, update the creation timestamp and add the csv headers
            var datalogFilePath = Path.Combine(datalogDirectory, Path.GetFileNameWithoutExtension(configFilePath) + ".csv");
            var csvFileExists = File.Exists(datalogFilePath);
            csvWriter = new CsvWriter(new StreamWriter(datalogFilePath, true, Encoding.UTF8), CultureInfo.InvariantCulture);

            if (!csvFileExists)
            {
                foreach (var column in configFile.Columns)
                {
                    csvWriter.WriteField(column.Header);
                }
                csvWriter.NextRecord();
                csvWriter.Flush();
            }
            else
            {
                //Load all the first in column values
                //firstValueInColumn = new ExpandoObject();
            }

            //Task.Run(async () =>
            //{
            //    await Preload();
            //    await Run();
            //}, subTaskCts.Token);
        }

        public async void WriteLine(Dictionary<string, JsonElement> keyValuePairs)
        {
            var now = DateTimeOffset.Now;
            dynamic serverExpando = new ExpandoObject();
            serverExpando.timestamp = now;
            serverExpando.timestampUtc = now.ToUniversalTime();
            var globals = new Dictionary<string, object>
            {
                { "server",  serverExpando},
                { "firstValueInColumn", firstValueInColumnExpando }
            };

            // Iterate through the API fields and parse, adding to the globals object
            foreach (var apiMapping in configFile.ApiFields)
            {
                if (keyValuePairs.ContainsKey(apiMapping.Field))
                {
                    var apiValue = keyValuePairs[apiMapping.Field].ToString();
                    switch (apiMapping.Type)
                    {
                        case DatalogFieldType.String:
                        {
                            globals.Add(apiMapping.Field, apiValue);
                            CheckAndAddFirstValueInColumn(apiMapping.Field, apiValue);
                            break;
                        }
                        case DatalogFieldType.Int32:
                        {
                            int value = int.Parse(apiValue);
                            globals.Add(apiMapping.Field, value);
                            CheckAndAddFirstValueInColumn(apiMapping.Field, value);
                            break;
                        }
                        case DatalogFieldType.Float:
                        {
                            var value = float.Parse(apiValue);
                            globals.Add(apiMapping.Field, value);
                            CheckAndAddFirstValueInColumn(apiMapping.Field, value);
                            break;
                        }
                        case DatalogFieldType.Double:
                        {
                            var value = double.Parse(apiValue);
                            globals.Add(apiMapping.Field, value);
                            CheckAndAddFirstValueInColumn(apiMapping.Field, value);
                            break;
                        }
                        case DatalogFieldType.Timestamp:
                        {
                            var value = DateTimeOffset.Parse(apiValue);
                            globals.Add(apiMapping.Field, value);
                            CheckAndAddFirstValueInColumn(apiMapping.Field, value);
                            break;
                        }
                        default:
                            throw new Exception("Unhandled column type");
                    }
                }
                else
                    throw new Exception("API field not found");
            }

            // Iterate through the Code fields, adding the result to the global object so the next code field in the list can use it
            foreach (var codeMapping in configFile.CodeFields)
            {
                switch (codeMapping.Type)
                {
                    case DatalogFieldType.String:
                    {
                        var result = await RunScript<string>(codeMapping, globals);
                        globals.Add(codeMapping.Field, result);
                        CheckAndAddFirstValueInColumn(codeMapping.Field, result);
                        break;
                    }
                    case DatalogFieldType.Int32:
                    {
                        var result = await RunScript<int>(codeMapping, globals);
                        globals.Add(codeMapping.Field, result);
                        CheckAndAddFirstValueInColumn(codeMapping.Field, result);
                        break;
                    }
                    case DatalogFieldType.Double:
                    {
                        var result = await RunScript<double>(codeMapping, globals);
                        globals.Add(codeMapping.Field, result);
                        CheckAndAddFirstValueInColumn(codeMapping.Field, result);
                        break;
                    }
                    case DatalogFieldType.Timestamp:
                    {
                        var result = await RunScript<DateTimeOffset>(codeMapping, globals);
                        globals.Add(codeMapping.Field, result);
                        CheckAndAddFirstValueInColumn(codeMapping.Field, result);
                        break;
                    }
                    default:
                        throw new Exception("Unhandled column type");
                }
            }

            // Now run the format for each column and write to CSV file
            foreach (var column in configFile.Columns)
            {
                string columnString;
                if (string.IsNullOrWhiteSpace(column.Format))
                    columnString = ((dynamic)globals[column.Field]).ToString();
                else
                    columnString = ((dynamic)globals[column.Field]).ToString(column.Format);
                csvWriter.WriteField(columnString);
            }
            csvWriter.NextRecord();
            csvWriter.Flush();
        }

        public void CheckAndAddFirstValueInColumn(string columnField, object data)
        {
            if (columnFields.Contains(columnField) && !firstValueInColumnHasBeenSet.Contains(columnField))
            {
                firstValueInColumnHasBeenSet.Add(columnField);
                ((IDictionary<string, object>)firstValueInColumnExpando).Add(columnField, data);
            }
        }

        public Task<T> RunScript<T>(DatalogConfigCodeField codeMapping, Dictionary<string, object> globals)
        {
            var typeInfo = globalTypeCache.Get(codeMapping.Field, globals);
            if (!scriptRunners.ContainsKey(codeMapping.Field))
                scriptRunners[codeMapping.Field] = StriptRunnerWithCustomGlobalType<T>.Create(codeMapping.Code, typeInfo);
            var runner = (ScriptRunner<T>)scriptRunners[codeMapping.Field];
            var instance = Activator.CreateInstance(typeInfo.Type, new object[] { globals });
            return runner.Invoke(instance);
        }

        private async Task Preload()
        {

        }

        private async Task Run()
        {

        }
    }
}
