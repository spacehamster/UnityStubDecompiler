using ICSharpCode.Decompiler.TypeSystem;
using System.Collections.Generic;

namespace UnityStubDecompiler
{
    public class DecompileType
    {
        public readonly ITypeDefinition TypeDefinition;
        public readonly DecompileModule Module;
        public IReadOnlyList<IField> Fields => m_Fields;
        private List<IField> m_Fields;
        public DecompileType(ITypeDefinition type, DecompileModule module, List<IField> fields)
        {
            TypeDefinition = type;
            Module = module;
            m_Fields = fields;
        }
        public string GetFilePath()
        {
            var typeParams = "";
            var namespacePart = "";
            if (!string.IsNullOrEmpty(TypeDefinition.Namespace))
            {
                namespacePart = TypeDefinition.Namespace + "\\";
            }
            if (TypeDefinition.TypeParameterCount > 0)
            {
                typeParams = $"_{TypeDefinition.TypeParameterCount}";
            }
            var relPath = $"{namespacePart}{TypeDefinition.Name}{typeParams}.cs";
            return $"{TypeDefinition.ParentModule.AssemblyName}/{relPath}";
        }
        public string Id => $"{TypeDefinition.FullTypeName.ToString()}, {TypeDefinition.ParentModule.AssemblyName}";
    }
}
