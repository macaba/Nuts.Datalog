using Microsoft.CodeAnalysis;
using System.Reflection;

namespace Nuts.Datalog.Scripting
{
    public class GlobalTypeInfo
    {
        public Assembly Assembly { get; init; }
        public MetadataReference Reference { get; init; }
        public Type Type { get; init; }
        public string Key { get; init; }
    }
}
