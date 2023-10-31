using System.Collections.Concurrent;

namespace Nuts.Datalog
{
    public class DatalogWriterCache
    {
        private readonly string configDirectory;
        private readonly string datalogDirectory;
        private readonly ConcurrentDictionary<string, DatalogWriter> datalogWriters = new();

        public DatalogWriterCache(string configDirectory, string datalogDirectory)
        {
            this.configDirectory = configDirectory;
            this.datalogDirectory = datalogDirectory;
        }

        public bool TryGetDatalogWriter(string name, out DatalogWriter writer)
        {
            if (datalogWriters.TryGetValue(name, out writer))
            {
                return true;
            }

            var configFilePath = Path.Combine(configDirectory, name + ".config");
            if (File.Exists(configFilePath))
            {
                writer = new DatalogWriter(configFilePath, datalogDirectory);
                datalogWriters.TryAdd(name, writer);

                return true;
            }
            return false;
        }
    }
}
