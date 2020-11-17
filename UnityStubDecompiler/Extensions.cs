
using ICSharpCode.Decompiler.TypeSystem;
using System.Linq;

namespace UnityStubDecompiler
{
    public static class Extensions
    {
        public static IType GetDirectBaseType(this IType type)
        {
            return type.DirectBaseTypes.Single(t => t.Kind != TypeKind.Interface && t.Kind != TypeKind.Unknown);
        }

        public static bool CompareType(this IType src, IType target)
        {
            //TODO: More robust type comparison
            return src.FullName == target.FullName;
        }

        public static bool CompareMethodSignature(this IMethod src, IMethod target)
        {
            if (src.IsStatic != target.IsStatic) return false;
            if (src.Name != target.Name) return false;
            if (!CompareType(src.ReturnType, target.ReturnType)) return false;
            if (src.Parameters.Count != target.Parameters.Count) return false;
            for(int i = 0; i < src.Parameters.Count; i++)
            {
                if (!CompareType(src.Parameters[i].Type, target.Parameters[i].Type)) return false;
            }
            return true;
        }

        public static bool ComparePropertySignature(this IProperty src, IProperty target)
        {
            if (src.IsStatic != target.IsStatic) return false;
            if (src.Name != target.Name) return false;
            if (!CompareType(src.ReturnType, target.ReturnType)) return false;
            if (src.Parameters.Count != target.Parameters.Count) return false;
            for (int i = 0; i < src.Parameters.Count; i++)
            {
                if (!CompareType(src.Parameters[i].Type, target.Parameters[i].Type)) return false;
            }
            return true;
        }
    }
}
