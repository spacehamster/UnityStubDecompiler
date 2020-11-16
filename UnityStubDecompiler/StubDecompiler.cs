using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.CSharp.Transforms;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.TypeSystem.Implementation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UnityStubDecompiler
{
    public class StubDecompiler
    {
        CSharpDecompiler decompiler;
        string projectDir;
        string ManagedDir;
        Options options;
        private Dictionary<string, DecompileType> m_TypeLookup;

        public StubDecompiler(Options options, string managedDir)
        {
            this.options = options;
            this.ManagedDir = managedDir;
        }

        public DecompileType GetType(ITypeDefinition type)
        {
            return m_TypeLookup[$"{type.FullTypeName}, {type.ParentModule.AssemblyName}"];
        }

        public bool HasType(ITypeDefinition type)
        {
            return m_TypeLookup.ContainsKey($"{type.FullTypeName}, {type.ParentModule.AssemblyName}");
        }

        List<IField> GetSerializedFields(IType mainType) {
            var result = new List<IField>();
            foreach(var field in mainType.GetFields())
            {
                if(!(field.Accessibility == Accessibility.Public || field.GetAttributes().Any(a => a.AttributeType.FullName == "UnityEngine.SerializeField"))){
                    continue;
                }
                var fieldType = field.Type;
                if (fieldType is ArrayType arrayType)
                {
                    fieldType = arrayType.ElementType;
                }
                if (fieldType is UnknownType unknownType)
                {
                    continue;
                }
                if (fieldType is AbstractTypeParameter abstractTypeParameter)
                {
                    continue;
                }
                var typeDefintion = fieldType.GetDefinition();
                if (!IsSerialized(typeDefintion)) continue;
                result.Add(field);
            }
            return result;

        }
        bool IsDerivedClass(ITypeDefinition type)
        {
            foreach(var baseType in type.GetNonInterfaceBaseTypes())
            {
                if (baseType == type) continue;
                if (baseType.FullName != "System.Object" && baseType.FullName != "System.ValueType") return true;
            }
            return false;
        }
        bool IsSerialized(ITypeDefinition type)
        {
            var baseTypes = type.GetNonInterfaceBaseTypes().Where(t => t != type).ToList();
            if(baseTypes.Any(t => (t.Name == "MonoBehaviour" || t.Name == "ScriptableObject" ) && t.Namespace == "UnityEngine"))
            {
                return true;
            }
            if (type.GetAttributes().Any(a => a.AttributeType.FullName == "System.SerializableAttribute") && type.ParentModule.AssemblyName == "mscorlib")
            {
                return true;
            }
            if (type.GetAttributes().Any(a => a.AttributeType.FullName == "System.SerializableAttribute") &&
                !IsDerivedClass(type) &&
                type.DeclaringType == null)
            {
                return true;
            }
            return false;
        }
        bool IsUnityModuleOrCoreLibrary(IModule module)
        {
            if (module.AssemblyName.StartsWith("UnityEngine")) return true;
            if (module.AssemblyName == "mscorlib") return true;
            if (module.AssemblyName == "System") return true;
            if (module.AssemblyName == "System.Core") return true;
            return false;
        }
        IEnumerable<ITypeDefinition> CollectTypes(IType type)
        {
            if (type is UnknownType unknownType)
            {
                yield break;
            }
            if (type is AbstractTypeParameter abstractTypeParameter)
            {
                yield break;
            }
            if (type is PointerType pointerType)
            {
                foreach (var elementType in CollectTypes(pointerType.ElementType))
                {
                    yield return elementType;
                }
                yield break;
            }
            if (type is ArrayType arrayType)
            {
                foreach(var elementType in CollectTypes(arrayType.ElementType))
                {
                    yield return elementType;
                }
                yield break;
            }
            if (type.GetDefinition() == null) throw new Exception();
            yield return type.GetDefinition();
        }
        CSharpDecompiler CreateDecompiler(string assemblyName)
        {
            var decompiler = new CSharpDecompiler($"{ManagedDir}/{assemblyName}.dll",
                new DecompilerSettings());
            decompiler.AstTransforms.Insert(0, new TypeFixer(this));
            decompiler.AstTransforms.Insert(0, new AttributeRemover());
            return decompiler;
        }
        void CollectTypes(out List<DecompileType> types, out List<DecompileModule> modules)
        {
            //TODO
            var result = new List<DecompileType>();
            var toCheck = new Stack<ITypeDefinition>(decompiler.TypeSystem.GetTopLevelTypeDefinitions());
            var seen = new HashSet<ITypeDefinition>();
            var moduleLookup = new Dictionary<IModule, DecompileModule>();
            while (toCheck.Count > 0)
            {
                var type = toCheck.Pop();
                if (seen.Contains(type)) continue;
                seen.Add(type);
                if (type.Name == "<Module>") continue;
                if (!IsSerialized(type)) continue;
                if (IsUnityModuleOrCoreLibrary(type.ParentModule)) continue;
                if (!moduleLookup.ContainsKey(type.ParentModule))
                {
                    var decompiler = CreateDecompiler(type.ParentModule.AssemblyName);
                    moduleLookup[type.ParentModule] = new DecompileModule(type.ParentModule, decompiler);
                }
                var module = moduleLookup[type.ParentModule];
                var fields = GetSerializedFields(type);
                foreach (var field in fields) 
                {
                    foreach (var fieldType in CollectTypes(field.Type))
                    {

                        if (IsUnityModuleOrCoreLibrary(fieldType.ParentModule))
                        {
                            continue;
                        }
                        if (!seen.Contains(fieldType))
                        {
                            toCheck.Push(fieldType);
                        }
                    }
                }
                result.Add(new DecompileType(type, module, fields));
            }

            types = result;
            modules = moduleLookup.Values.ToList();
        }
        public static void DecompileProject(string managedDir, Options options)
        {
            var blueprintDecompiler = new StubDecompiler(options, managedDir);
            blueprintDecompiler.Decompile();
        }
        void Decompile()
        {
            projectDir = options.SolutionDirectoryName;
            decompiler = CreateDecompiler("Assembly-CSharp");
            CollectTypes(out List<DecompileType> types, out List<DecompileModule> modules);
            m_TypeLookup = new Dictionary<string, DecompileType>();
            foreach(var type in types)
            {
                if (m_TypeLookup.ContainsKey(type.Id))
                {
                    var dup = m_TypeLookup[type.Id];
                    var test = type.TypeDefinition == dup.TypeDefinition;
                }
                m_TypeLookup.Add(type.Id, type);
            }
            var filenameTypeLookup = new Dictionary<string, DecompileType>();
            var topTypes = types.Where(t => t.TypeDefinition.DeclaringType == null);
            foreach (var type in topTypes)
            {
                var path = $"{projectDir}/{type.GetFilePath()}";
                if (filenameTypeLookup.ContainsKey(path))
                {
                    var other = filenameTypeLookup[path];
                    throw new Exception($"Duplicate file names");
                } else
                {
                    filenameTypeLookup.Add(path, type);
                }
                DecompileType(type);
            }
            if (options.GenerateSolution)
            {
                SolutionGenerator.Generate(options, modules);
            }
        }
        public void DecompileType(DecompileType type)
        {
            var path = $"{projectDir}/{type.GetFilePath()}";
            FileUtil.EnsureDirectory(Path.GetDirectoryName(path));
            var fullName = type.TypeDefinition.FullTypeName;
            try
            {
                var text = type.Module.Decompiler.DecompileTypeAsString(fullName);
                File.WriteAllText(path, text);
            } catch(Exception ex)
            {
                File.WriteAllText(path, $"/*\n{ex}\n*/");
            }
            
        }
    }
}
