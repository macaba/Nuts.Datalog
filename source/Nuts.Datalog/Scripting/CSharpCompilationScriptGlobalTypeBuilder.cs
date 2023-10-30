using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Reflection;

namespace Nuts.Datalog.Scripting
{
    /// <summary>
    /// Using ReflectionEmit with Microsoft.CodeAnalysis.CSharp.Scripting is not supported. 
    /// The workaround is to use CSharpCompilation instead.
    /// ref: https://github.com/dotnet/roslyn/issues/2246
    /// ref: https://github.com/dotnet/roslyn/pull/6254
    /// </summary>
    public class CSharpCompilationScriptGlobalTypeBuilder
    {

        private const string TEMPLATE = @"
using System;
using System.Collections.Generic;
public class {0}
{{
    public {0}(
        IDictionary<string, Object> extensions)
    {{
        {1}
    }}
    {2}
}}";

        private int unique = 0;
        private readonly IDictionary<string, GlobalTypeInfo> _cache;

        public CSharpCompilationScriptGlobalTypeBuilder()
        {
            _cache = new Dictionary<string, GlobalTypeInfo>();
        }

        private static PortableExecutableReference GetMetadataReference(Type type)
        {
            var assemblyLocation = type.Assembly.Location;
            return MetadataReference.CreateFromFile(assemblyLocation);
        }

        public GlobalTypeInfo Create(string key, IDictionary<string, object> extensions)
        {
            // No locking. the worst that happens is we generate the type
            // multiple times and throw all but one away. 
            if (!_cache.TryGetValue(key, out var item))
            {
                item = CreateCore(key, extensions.ToDictionary(x => x.Key, x => x.Value.GetType()));
                _cache[key] = item;
            }
            return item;
        }

        private GlobalTypeInfo CreateCore(string key, IDictionary<string, Type> extensionDetails)
        {
            var count = Interlocked.Increment(ref unique);
            var typeName = $"DynamicType{count}";

            var code = string.Format(TEMPLATE,
                typeName,
                string.Join(System.Environment.NewLine, extensionDetails.Select(pair => $"{pair.Key} = ({pair.Value.FullName})extensions[\"{pair.Key}\"];")),
                string.Join(System.Environment.NewLine, extensionDetails.Select(pair => $"public {pair.Value.FullName} {pair.Key} {{ get; }}")));


            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);

            //TODO: need to change the way references are being added.
            // See these links for how the preferred method:
            // ref: https://github.com/dotnet/roslyn/issues/49498
            // ref: https://github.com/dotnet/roslyn/issues/49498#issuecomment-776059232
            // ref: https://github.com/jaredpar/basic-reference-assemblies
            // ref: https://stackoverflow.com/q/32961592/2076531
            PortableExecutableReference[] references = Assembly.GetEntryAssembly().GetReferencedAssemblies()
                .Select(a => MetadataReference.CreateFromFile(Assembly.Load(a).Location))
                .Concat(extensionDetails.Values.Select(GetMetadataReference))
                .Append(GetRuntimeSpecificReference())
                .Append(GetMetadataReference(typeof(CSharpCompilationScriptGlobalTypeBuilder)))
                .Append(GetMetadataReference(typeof(System.Linq.Enumerable)))
                .Append(GetMetadataReference(typeof(System.Runtime.AssemblyTargetedPatchBandAttribute)))
                .Append(GetMetadataReference(typeof(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo)))
                .Append(GetMetadataReference(typeof(System.Collections.Generic.IDictionary<string, object>)))
                .Append(GetMetadataReference(typeof(object)))
                .Append(GetMetadataReference(typeof(GlobalTypeInfo)))
                .ToArray();



            Compilation compilation = CSharpCompilation.Create(
                $"ScriptGlobalTypeBuilder{count}", new[] { syntaxTree }, references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            ImmutableArray<byte> assemblyBytes = compilation.EmitToArray();
            PortableExecutableReference libRef = MetadataReference.CreateFromImage(assemblyBytes);
            Assembly assembly = Assembly.Load(assemblyBytes.ToArray());

            return new GlobalTypeInfo()
            {
                Key = key,
                Assembly = assembly,
                Reference = libRef,
                Type = assembly.GetType(typeName),
            };
        }


        private static PortableExecutableReference GetRuntimeSpecificReference()
        {
            var assemblyLocation = typeof(object).Assembly.Location;
            var runtimeDirectory = Path.GetDirectoryName(assemblyLocation);
            var libraryPath = Path.Join(runtimeDirectory, @"netstandard.dll");

            return MetadataReference.CreateFromFile(libraryPath);
        }
    }
}
