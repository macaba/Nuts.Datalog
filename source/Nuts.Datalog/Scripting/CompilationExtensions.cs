using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    public static class CompilationExtensions
    {
        public static ImmutableArray<byte> EmitToArray(this Compilation compilation)
        {
            using (MemoryStream assemblyStream = new MemoryStream())
            {
                Emit.EmitResult emitResult = compilation.Emit(assemblyStream);

                if (emitResult.Success)
                {
                    return ImmutableArray.Create<byte>(assemblyStream.ToArray());
                }

                var errors = emitResult
                    .Diagnostics
                    .Select(diagnostic => diagnostic.GetMessage())
                    .Select(message => new Exception(message));

                throw new AggregateException(errors);
            }
        }
    }
}