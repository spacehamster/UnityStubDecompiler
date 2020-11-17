
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

        public static bool CompareMethodSignature(this IMethod src, IMethod target)
        {
            if (src.IsStatic != target.IsStatic) return false;
            if (src.Name != target.Name) return false;
            if (src.ReturnType.FullName != target.ReturnType.FullName) return false;
            if (src.Parameters.Count != target.Parameters.Count) return false;
            for(int i = 0; i < src.Parameters.Count; i++)
            {
                if (src.Parameters[i].Type.FullName != target.Parameters[i].Type.FullName) return false;
            }
            return true;
        }
    }
}
