using CsvHelper;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Nuts.Datalog.Scripting;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Nuts.Datalog
{
    public class DatalogWriter
    {
        private readonly string jsonFilePath;
        private readonly CSharpCompilationScriptGlobalTypeBuilder factory;

        private readonly DatalogJsonConfiguration jsonFile;
        private readonly CsvWriter csvWriter;

        private Dictionary<string, ScriptRunner<string>> scriptRunners = new();

        public DatalogWriter(string jsonFilePath, CSharpCompilationScriptGlobalTypeBuilder factory)
        {
            this.jsonFilePath = jsonFilePath;
            this.factory = factory;
            var json = File.ReadAllText(jsonFilePath);

            // Deserialise the JSON configuration
            jsonFile = DatalogJsonConfiguration.ReadFromDisk(jsonFilePath);
            if (jsonFile is null)
                throw new ApplicationException($"Could not deserialize {Path.GetFileName(jsonFilePath)}");

            // Create the CSV file, update the creation timestamp and add the csv headers
            var datalogFilePath = Path.Combine(Path.GetDirectoryName(jsonFilePath), Path.GetFileNameWithoutExtension(jsonFilePath) + ".csv");
            var csvFileExists = File.Exists(datalogFilePath);
            csvWriter = new CsvWriter(new StreamWriter(datalogFilePath, true, Encoding.UTF8), CultureInfo.InvariantCulture);

            if (!csvFileExists)
            {
                jsonFile.Metadata.CreationTimestampUtc = DateTimeOffset.UtcNow;
                jsonFile.WriteToDisk(jsonFilePath);
                
                foreach (var column in jsonFile.Columns)
                {
                    csvWriter.WriteField(column.Name);
                }
                csvWriter.NextRecord();
                csvWriter.Flush();
            }

            if (jsonFile.Metadata.CreationTimestampUtc is null)
            {
                throw new Exception("CreationTimestampUtc is invalid");
            }
        }

        public async void WriteLine(Dictionary<string, string> keyValuePairs)
        {
            // Build up the globals object for the string interpolation
            var globals = new Dictionary<string, object>();
            var now = DateTimeOffset.Now;
            globals.Add("ServerTimestamp", now);
            globals.Add("ServerTimestampExcel", now.ToString("yyyy-MM-dd HH:mm:ss"));
            globals.Add("ServerTimestampUtc", now.ToUniversalTime());
            globals.Add("ServerTimestampUtcExcel", now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss"));
            globals.Add("ServerSecondsSinceCreation", now.ToUniversalTime().Subtract((DateTimeOffset)jsonFile.Metadata.CreationTimestampUtc).TotalSeconds);

            // Iterate through the API map and parse, adding to the globals object
            foreach (var apiMapping in jsonFile.ApiFields)
            {
                if (keyValuePairs.ContainsKey(apiMapping.ApiField))
                {
                    var apiValue = keyValuePairs[apiMapping.ApiField];
                    switch (apiMapping.Type)
                    {
                        case DatalogType.String:
                        {
                            globals.Add(apiMapping.ApiField, apiValue);
                            break;
                        }
                        case DatalogType.Int32:
                        {
                            int value = int.Parse(apiValue);
                            globals.Add(apiMapping.ApiField, value);
                            break;
                        }
                        case DatalogType.Float:
                        {
                            var value = float.Parse(apiValue);
                            globals.Add(apiMapping.ApiField, value);
                            break;
                        }
                        case DatalogType.Double:
                        {
                            var value = double.Parse(apiValue);
                            globals.Add(apiMapping.ApiField, value);
                            break;
                        }
                        case DatalogType.Timestamp:
                        {
                            var value = DateTimeOffset.Parse(apiValue);
                            globals.Add(apiMapping.ApiField, value);
                            break;
                        }
                        default:
                            throw new Exception("Unhandled column type");
                    }
                }
                else
                    throw new Exception("API field not found");
            }

            GlobalTypeInfo typeInfo = factory.Create(jsonFilePath, globals);
            if (scriptRunners.Count == 0)
            {
                // Build up the scriptRunners dictionary                
                foreach (var column in jsonFile.Columns)
                {
                    var code = "return $\"" + column.StringInterpolation + "\";";
                    scriptRunners.Add(column.Name, CreateRunner(code, typeInfo));
                }
            }

            // Now run the string interpolation for each column
            foreach (var column in jsonFile.Columns)
            {
                var instance = Activator.CreateInstance(typeInfo.Type, new object[] { globals });
                var result = await scriptRunners[column.Name].Invoke(instance);
                csvWriter.WriteField(result);
            }
            csvWriter.NextRecord();
            csvWriter.Flush();
        }


        private async Task<ScriptRunner<string>> CreateRunnerAsync(string scriptContent, GlobalTypeInfo typeInfo)
        {

            //ref: https://github.com/dotnet/roslyn/blob/main/docs/wiki/Scripting-API-Samples.md
            var options = ScriptOptions.Default
                .AddImports("System")
                .AddImports("System.Text");

            //TODO: this is overkill find a better way to do this.
            var assemblies = Assemblies.ApplicationDependencies()
                .SelectMany(assembly => assembly.GetExportedTypes())
                .Select(type => type.Assembly)
                .Distinct();
            options.AddReferences(assemblies);

            using (var loader = new InteractiveAssemblyLoader())
            {
                loader.RegisterDependency(typeInfo.Assembly);

                var script = CSharpScript.Create<string>(
                    scriptContent,
                    options.WithReferences(typeInfo.Reference),
                    globalsType: typeInfo.Type,
                    assemblyLoader: loader);

                return script.CreateDelegate();
            }
        }

        private ScriptRunner<string> CreateRunner(string scriptContent, GlobalTypeInfo typeInfo)
        {

            //ref: https://github.com/dotnet/roslyn/blob/main/docs/wiki/Scripting-API-Samples.md
            var options = ScriptOptions.Default
                .AddImports("System")
                .AddImports("System.Text");

            //TODO: this is overkill find a better way to do this.
            var assemblies = Assemblies.ApplicationDependencies()
                .SelectMany(assembly => assembly.GetExportedTypes())
                .Select(type => type.Assembly)
                .Distinct();
            options.AddReferences(assemblies);

            using (var loader = new InteractiveAssemblyLoader())
            {
                loader.RegisterDependency(typeInfo.Assembly);

                var script = CSharpScript.Create<string>(
                    scriptContent,
                    options.WithReferences(typeInfo.Reference),
                    globalsType: typeInfo.Type,
                    assemblyLoader: loader);

                return script.CreateDelegate();
            }
        }
    }
}
