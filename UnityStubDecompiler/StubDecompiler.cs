using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
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

        ITypeDefinition GetConcreteFieldType(IType type)
        {
            if (type is UnknownType unknownType)
            {
                return null;
            }
            if (type is AbstractTypeParameter abstractTypeParameter)
            {
                return null;
            }
            if (type is ArrayType arrayType)
            {
                return GetConcreteFieldType(arrayType.ElementType);
            }
            if (type.TypeArguments.Count > 0 && type.FullName == "System.Collections.Generic.List")
            {
                return GetConcreteFieldType(type.TypeArguments[0]);
            }
            if (type.TypeArguments.Count > 0)
            {
                return null;
            }
            return type.GetDefinition();
        }

        List<IField> GetSerializedFields(ITypeDefinition mainType) {
            var result = new List<IField>();
            if (!IsSerialized(mainType))
            {
                return result;
            }
            foreach(var field in mainType.GetFields())
            {
                if(!(field.Accessibility == Accessibility.Public || field.GetAttributes().Any(a => a.AttributeType.FullName == "UnityEngine.SerializeField"))){
                    continue;
                }
                var fieldType = GetConcreteFieldType(field.Type);
                if (fieldType == null) continue;
                if (!IsSerialized(fieldType)) continue;
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
        bool IsMonobehaviourOrScriptableObject(ITypeDefinition type)
        {
            var baseTypes = type.GetNonInterfaceBaseTypes().Where(t => t != type).ToList();
            if (baseTypes.Any(t => (t.Name == "MonoBehaviour" || t.Name == "ScriptableObject") && t.Namespace == "UnityEngine"))
            {
                return true;
            }
            return false;
        }
        bool IsSerialized(ITypeDefinition type)
        {
            if(IsMonobehaviourOrScriptableObject(type))
            {
                return true;
            }
            if (type.GetAttributes().Any(a => a.AttributeType.FullName == "System.SerializableAttribute") && type.ParentModule.AssemblyName == "mscorlib")
            {
                return true;
            }
            if (type.GetAttributes().Any(a => a.AttributeType.FullName == "System.SerializableAttribute") &&
                !IsDerivedClass(type))
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
        bool IsUnityModuleOrCoreLibrary(IModule module)
        {
            if (IsUnityModule(module)) return true;
            if (module.AssemblyName == "mscorlib") return true;
            if (module.AssemblyName == "System") return true;
            if (module.AssemblyName == "System.Core") return true;
            if (module.AssemblyName == "System.Xml") return true;
            if (module.AssemblyName == "System.Xml.Linq") return true;
            return false;
        }
        IEnumerable<ITypeDefinition> CollectTypes(IType type, bool includeSelf = true, bool includeBaseType = false)
        {
            //TODO: Determine why there are unknown types
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
            foreach(var typeArgument in type.TypeArguments.SelectMany(arg => CollectTypes(arg)))
            {
                yield return typeArgument;
            }
            if(type.DeclaringType != null)
            {
                foreach(var declaringType in CollectTypes(type.DeclaringType))
                {
                    yield return declaringType;
                }
            }
            if (includeSelf)
            {
                if (type.GetDefinition() == null) throw new Exception();
                yield return type.GetDefinition();
            }
            if (includeBaseType)
            {
                var baseType = type.GetDirectBaseType();
                if (baseType != null)
                {
                    foreach (var baseTypeRef in CollectTypes(baseType))
                    {
                        yield return baseTypeRef;
                    }
                }
            }
        }
        CSharpDecompiler CreateDecompiler(string assemblyName)
        {
            var decompiler = new CSharpDecompiler($"{ManagedDir}/{assemblyName}.dll",
                new DecompilerSettings());
            decompiler.AstTransforms.Insert(0, new TypeFixer(this));
            decompiler.AstTransforms.Insert(0, new AttributeRemover());
            return decompiler;
        }
        List<IMethod> GetAbstractMethodImplementations(ITypeDefinition type)
        {
            var abstractMethods = new List<IMethod>();
            var baseTypes = type.GetNonInterfaceBaseTypes().ToArray();
            foreach (var baseType in baseTypes)
            {
                //TODO: determine why there are unknown types
                if (baseType is UnknownType) continue;
                var def = baseType.GetDefinition();
                if (!IsUnityModule(def.ParentModule)) continue;
                foreach(var method in def.Methods)
                {
                    if (method.IsAbstract)
                    {
                        abstractMethods.Add(method);
                    }
                }
            }
            var result = new List<IMethod>();
            if (abstractMethods.Count == 0) return result;
            foreach(var method in type.Methods)
            {
                foreach(var abstractMethod in abstractMethods)
                {
                    if (method.CompareMethodSignature(abstractMethod))
                    {
                        result.Add(method);
                    }
                }
            }
            return result;
        }
        List<IProperty> GetAbstractPropertyImplementations(ITypeDefinition type)
        {
            var abstractProperties = new List<IProperty>();
            var baseTypes = type.GetNonInterfaceBaseTypes().ToArray();
            foreach (var baseType in baseTypes)
            {
                //TODO: determine why there are unknown types
                if (baseType is UnknownType) continue;
                var def = baseType.GetDefinition();
                if (!IsUnityModule(def.ParentModule)) continue;
                foreach (var property in def.Properties)
                {
                    if (property.IsAbstract)
                    {
                        abstractProperties.Add(property);
                    }
                }
            }
            var result = new List<IProperty>();
            if (abstractProperties.Count == 0) return result;
            foreach (var property in type.Properties)
            {
                foreach (var abstractProperty in abstractProperties)
                {
                    if (property.ComparePropertySignature(abstractProperty))
                    {
                        result.Add(property);
                    }
                }
            }
            return result;
        }
        IEnumerable<ITypeDefinition> CollectTypes(IMethod method)
        {
            foreach (var parameter in method.Parameters)
            {
                foreach(var type in CollectTypes(parameter.Type))
                {
                    yield return type;
                }
            }
            foreach (var type in CollectTypes(method.ReturnType))
            {
                yield return type;
            }
        }
        IEnumerable<ITypeDefinition> CollectTypes(IField field)
        {
            foreach (var type in CollectTypes(field.Type))
            {
                yield return type;
            }
            foreach (var attribute in field.GetAttributes())
            {
                if (attribute.AttributeType.FullName == "UnityEngine.SerializeField")
                {
                    foreach (var type in CollectTypes(attribute.AttributeType))
                    {
                        yield return type;
                    }
                }
            }
        }
        void CollectTypes(out List<DecompileType> types, out List<DecompileModule> modules)
        {
            var result = new List<DecompileType>();
            //Skip compiler generated types
            var initialTypes = decompiler.TypeSystem.GetTopLevelTypeDefinitions()
                .Where(t => IsMonobehaviourOrScriptableObject(t));
            var toCheck = new Stack<ITypeDefinition>(initialTypes);
            var seen = new HashSet<ITypeDefinition>();
            var moduleLookup = new Dictionary<IModule, DecompileModule>();
            while (toCheck.Count > 0)
            {
                var type = toCheck.Pop();
                if (seen.Contains(type)) continue;
                seen.Add(type);
                if (type.Name == "<Module>") continue;
                if (IsUnityModuleOrCoreLibrary(type.ParentModule)) continue;
                if (!moduleLookup.ContainsKey(type.ParentModule))
                {
                    var decompiler = CreateDecompiler(type.ParentModule.AssemblyName);
                    moduleLookup[type.ParentModule] = new DecompileModule(type.ParentModule, decompiler);
                }
                var module = moduleLookup[type.ParentModule];
                var typeReferences = CollectTypes(type, includeSelf: false, includeBaseType: true).ToList();
                foreach (var typeReference in typeReferences) 
                {
                    toCheck.Push(typeReference);
                    if (typeReference.ParentModule != module.Module)
                    {
                        module.AddReference(typeReference.ParentModule);
                    }
                }

                var methods = GetAbstractMethodImplementations(type);
                foreach(var method in methods)
                {
                    foreach(var methodType in CollectTypes(method))
                    {
                        if (methodType.ParentModule != type.ParentModule)
                        {
                            module.AddReference(methodType.ParentModule);
                        }
                        if (IsUnityModuleOrCoreLibrary(methodType.ParentModule))
                        {
                            continue;
                        }
                        if (!seen.Contains(methodType))
                        {
                            toCheck.Push(methodType);
                        }
                    }
                }

                var properties = GetAbstractPropertyImplementations(type);
                //TODO: Collect property types

                var fields = GetSerializedFields(type);
                foreach (var field in fields) 
                {
                    foreach (var fieldType in CollectTypes(field))
                    {
                        if(fieldType.ParentModule != type.ParentModule)
                        {
                            module.AddReference(fieldType.ParentModule);
                        }
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
                result.Add(new DecompileType(type, module, fields, methods, properties));
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
            Console.WriteLine("Collecting Types");
            projectDir = options.SolutionDirectoryName;
            decompiler = CreateDecompiler("Assembly-CSharp");
            CollectTypes(out List<DecompileType> types, out List<DecompileModule> modules);
            m_TypeLookup = new Dictionary<string, DecompileType>();
            foreach(var type in types)
            {
                m_TypeLookup.Add(type.Id, type);
            }
            var filenameTypeLookup = new Dictionary<string, DecompileType>();
            var topTypes = types
                .Where(t => t.TypeDefinition.DeclaringType == null)
                .GroupBy(t => t.Module)
                .OrderBy(g => g.Key.Module.AssemblyName);
            foreach (var group in topTypes)
            {
                Console.WriteLine($"Decompiling {group.Key.Module.AssemblyName}");
                foreach (var type in group)
                {
                    var path = $"{projectDir}/{type.GetFilePath()}";
                    if (filenameTypeLookup.ContainsKey(path))
                    {
                        var other = filenameTypeLookup[path];
                        throw new Exception($"Duplicate file names");
                    }
                    else
                    {
                        filenameTypeLookup.Add(path, type);
                    }
                    DecompileType(type);
                }
            }
            if (options.GenerateSolution)
            {
                SolutionGenerator.Generate(ManagedDir, options, modules);
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
                Console.WriteLine($"Error decompiling type {type.TypeDefinition.FullName}. {ex.GetType().Name}");
                //Temp hack. todo: fix ILSpy decompile error
                using var sw = new StreamWriter(path);
                sw.WriteLine("/* Decompile Error");
                sw.WriteLine(ex.ToString());
                sw.WriteLine("*/");
                sw.WriteLine($"namespace {type.TypeDefinition.Namespace} {{");
                sw.WriteLine($"    public class {type.TypeDefinition.Name} {{");
                sw.WriteLine("    }");
                sw.WriteLine("}");
            }
            
        }
    }
}
