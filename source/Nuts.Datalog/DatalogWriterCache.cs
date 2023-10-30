using Nuts.Datalog.Scripting;
using System.Collections.Concurrent;

namespace Nuts.Datalog
{
    public class DatalogWriterCache
    {
        private readonly string directory;
        private readonly ConcurrentDictionary<string, DatalogWriter> datalogWriters = new();
        CSharpCompilationScriptGlobalTypeBuilder factory = new();

        public DatalogWriterCache(string directory)
        {
            this.directory = directory;
        }

        public bool TryGetDatalogWriter(string name, out DatalogWriter writer)
        {
            if (datalogWriters.TryGetValue(name, out writer))
            {
                return true;
            }

            var jsonFilePath = Path.Combine(directory, name + ".json");
            if (File.Exists(jsonFilePath))
            {
                writer = new DatalogWriter(jsonFilePath, factory);
                datalogWriters.TryAdd(name, writer);

                return true;
            }
            return false;
        }
    }
}
