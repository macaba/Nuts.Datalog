using Microsoft.Extensions.DependencyModel;

namespace System.Reflection
{
    public static class Assemblies
    {
        public static IEnumerable<Assembly> ApplicationDependencies(Func<AssemblyName, bool> predicate = null)
        {
            if (predicate == null)
            {
                predicate = _ => true;
            }
            try
            {
                return FromDependencyContext(DependencyContext.Default, predicate);
            }
            catch
            {
                // Something went wrong when loading the DependencyContext, fall
                // back to loading all referenced assemblies of the entry assembly...
                return FromAssemblyDependencies(Assembly.GetEntryAssembly(), predicate);
            }
        }

        private static IEnumerable<Assembly> FromDependencyContext(DependencyContext context, Func<AssemblyName, bool> predicate)
        {
            var assemblyNames = context.RuntimeLibraries
                .SelectMany(library => library.GetDefaultAssemblyNames(context));
            return LoadAssemblies(assemblyNames, predicate);
        }

        private static IEnumerable<Assembly> FromAssemblyDependencies(Assembly assembly, Func<AssemblyName, bool> predicate)
        {
            var dependencyNames = assembly.GetReferencedAssemblies();
            var results = LoadAssemblies(dependencyNames, predicate);
            if (predicate(assembly.GetName()))
            {
                results.Prepend(assembly);
            }
            return results;
        }

        private static IEnumerable<Assembly> LoadAssemblies(IEnumerable<AssemblyName> assemblyNames, Func<AssemblyName, bool> predicate)
        {
            var assemblies = new List<Assembly>();

            foreach (var assemblyName in assemblyNames.Where(predicate))
            {
                try
                {
                    // Try to load the referenced assembly...
                    assemblies.Add(Assembly.Load(assemblyName));
                }
                catch
                {
                    // Failed to load assembly. Skip it.
                }
            }

            return assemblies;
        }
    }
}