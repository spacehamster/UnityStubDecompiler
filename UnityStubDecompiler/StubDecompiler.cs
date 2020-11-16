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
            return mainType.GetFields().ToList();
        }
        bool IsSerialized(ITypeDefinition type)
        {
            var baseType = type.GetNonInterfaceBaseTypes().Where(t => t != type).ToList();
            if(baseType.Any(t => (t.Name == "MonoBehaviour" || t.Name == "ScriptableObject" ) && t.Namespace == "UnityEngine"))
            {
                return true;
            }
            return false;
        }
        bool IsUnityModule(IModule module)
        {
            if (module.AssemblyName.StartsWith("UnityEngine")) return true;
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
                if (IsUnityModule(type.ParentModule)) continue;
                foreach (var dep in CollectTypes(type))
                {
                    //toCheck.Push(dep);
                }
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

                        if (IsUnityModule(fieldType.ParentModule))
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
            var text = type.Module.Decompiler.DecompileTypeAsString(type.TypeDefinition.FullTypeName);
            File.WriteAllText(path, text);
        }
    }
}
