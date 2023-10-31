using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using System.Reflection;

namespace Nuts.Datalog.Scripting
{
    public static class StriptRunnerWithCustomGlobalType<T>
    {
        //private async Task<ScriptRunner<string>> CreateAsync(string scriptContent, GlobalTypeInfo typeInfo)
        //{

        //    //ref: https://github.com/dotnet/roslyn/blob/main/docs/wiki/Scripting-API-Samples.md
        //    var options = ScriptOptions.Default
        //        .AddImports("System")
        //        .AddImports("System.Text");

        //    //TODO: this is overkill find a better way to do this.
        //    var assemblies = Assemblies.ApplicationDependencies()
        //        .SelectMany(assembly => assembly.GetExportedTypes())
        //        .Select(type => type.Assembly)
        //        .Distinct();
        //    options.AddReferences(assemblies);

        //    using (var loader = new InteractiveAssemblyLoader())
        //    {
        //        loader.RegisterDependency(typeInfo.Assembly);

        //        var script = CSharpScript.Create<string>(
        //            scriptContent,
        //            options.WithReferences(typeInfo.Reference),
        //            globalsType: typeInfo.Type,
        //            assemblyLoader: loader);

        //        return script.CreateDelegate();
        //    }
        //}

        public static ScriptRunner<T> Create(string scriptContent, GlobalTypeInfo typeInfo)
        {
            //var assemblies = Assemblies.ApplicationDependencies()
            //    .SelectMany(assembly => assembly.GetExportedTypes())
            //    .Select(type => type.Assembly)
            //    .Distinct();

            var imports = new[]
            {
                "System",
                "System.Text",
                "System.Math"
            };

            var assemblies = new[]
            {
                typeof(object).GetTypeInfo().Assembly,                              // System.Private.CoreLib
                typeof(System.Linq.Enumerable).GetTypeInfo().Assembly,              // System.Linq
                typeof(System.Dynamic.ExpandoObject).Assembly,                      // System.Linq.Expressions
                typeof(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo).Assembly, // Microsoft.CSharp
            };

            //ref: https://github.com/dotnet/roslyn/blob/main/docs/wiki/Scripting-API-Samples.md
            var options = ScriptOptions.Default
                .AddImports(imports)
                .AddReferences(assemblies)
                .AddReferences(typeInfo.Reference);

            var scriptContentModified = "using Timestamp = System.DateTimeOffset; " + scriptContent;

            using (var loader = new InteractiveAssemblyLoader())
            {
                loader.RegisterDependency(typeInfo.Assembly);

                var script = CSharpScript.Create<T>(
                    scriptContentModified,
                    options,
                    globalsType: typeInfo.Type,
                    assemblyLoader: loader);

                return script.CreateDelegate();
            }
        }
    }
}
