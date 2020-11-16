
using ICSharpCode.Decompiler.TypeSystem;
using System.Linq;

namespace UnityStubDecompiler
{
    public static class Extensions
    {
        public static IType GetDirectBaseType(this IType type)
        {
            return type.DirectBaseTypes.Single(t => t.Kind != TypeKind.Interface);
        }
    }
}
