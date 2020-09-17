using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.CSharp.Transforms;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;

namespace Decompile
{
    public class AttributeRemover : DepthFirstAstVisitor, IAstTransform
    {
        public override void VisitAttributeSection(AttributeSection attribute)
        {
            attribute.Remove();
        }
        public void Run(AstNode rootNode, TransformContext context)
        {
            rootNode.AcceptVisitor(this);
        }
    }
    public class TypeFixer : DepthFirstAstVisitor, IAstTransform
    {
        CSharpDecompiler Decompiler;
        Dictionary<ITypeDefinition, List<IField>> Types;
        public TypeFixer(CSharpDecompiler decompiler, Dictionary<ITypeDefinition, List<IField>> types)
        {
            Decompiler = decompiler;
            Types = types;
        }
        public override void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
        {
            var baseTypes = typeDeclaration.BaseTypes.ToArray();
            foreach (var baseNode in baseTypes)
            {
                var resolve = baseNode.GetResolveResult();
                if (resolve.Type.Kind == TypeKind.Interface)
                {
                    baseNode.Remove();
                }
            }
            var members = typeDeclaration.Members.ToArray();
            foreach(var subclass in typeDeclaration.Children.OfType<TypeDeclaration>())
            {
                var resolve = subclass.GetResolveResult();
                if (!Types.ContainsKey(resolve.Type.GetDefinition()))
                {
                    subclass.Remove();
                }
                else
                {
                    VisitTypeDeclaration(subclass);
                }
            }
            foreach (var member in typeDeclaration.Members)
            {
                if(member is MethodDeclaration)
                {
                    member.Remove();
                }
                if (member is PropertyDeclaration)
                {
                    member.Remove();
                }
                if (member is OperatorDeclaration)
                {
                    member.Remove();
                }
                if (member is IndexerDeclaration)
                {
                    member.Remove();
                }
                if (member is ConstructorDeclaration)
                {
                    member.Remove();
                }
                if (member is FieldDeclaration fd)
                {
                    var fields = Types[typeDeclaration.GetResolveResult().Type.GetDefinition()];
                    if (!fields.Contains(fd.GetSymbol()))
                    {
                        member.Remove();
                    }
                }
            }
        }

        public void Run(AstNode rootNode, TransformContext context)
        {
            rootNode.AcceptVisitor(this);
        }
    }
    class ScriptStubDecompiler
    {
        CSharpDecompiler decompiler;
        string projectDir;
        List<IField> GetSerializedFields(IType mainType) {
            return mainType.GetFields().ToList();
        }
        ITypeDefinition TypeByFullname(string fullName)
        {
            var name = new FullTypeName(fullName);
            var type = decompiler.TypeSystem.MainModule.Compilation.FindType(name).GetDefinition();
            return type;
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
        void CollectTypes(out Dictionary<ITypeDefinition, List<IField>> result, out HashSet<IModule> dependentModules)
        {
            //TODO
            var lookup = new Dictionary<ITypeDefinition, List<IField>>();
            foreach (var type in decompiler.TypeSystem.GetTopLevelTypeDefinitions())
            {
                if (type.Name == "<Module>") continue;
                if (IsSerialized(type) && type.ParentModule.Name == "Assembly-CSharp")
                {
                    lookup[type] = GetSerializedFields(type);
                }
            }
            HashSet<IModule> modules = new HashSet<IModule>();
            dependentModules = modules;
            result = lookup;
        }
        public static void DecompileBlueprintProject(string managedDir)
        {
            var blueprintDecompiler = new ScriptStubDecompiler();
            blueprintDecompiler.Decompile(managedDir);
        }
        void Decompile(string managedDir)
        {
            projectDir = $"Scripts";
            var settings = new DecompilerSettings();
            decompiler = new CSharpDecompiler($"{managedDir}/Assembly-CSharp.dll",
                new DecompilerSettings());
            CollectTypes(out Dictionary<ITypeDefinition, List<IField>> types, out HashSet<IModule> modules);
            decompiler.AstTransforms.Insert(0, new TypeFixer(decompiler, types));
            decompiler.AstTransforms.Insert(0, new AttributeRemover());
            var foo = new Dictionary<string, ITypeDefinition>();
            var topTypes = types.Keys.Where(t => t.DeclaringType == null);
            foreach (var type in topTypes)
            {
                if (foo.ContainsKey(ToFileName(type)))
                {
                    var other = foo[ToFileName(type)];
                    throw new Exception($"Duplicate file names");
                } else
                {
                    foo.Add(ToFileName(type), type);
                }
                DecompileType(type);
            }
        }
        public void DecompileType(ITypeDefinition type)
        {
            var path = ToFileName(type);
            FileUtil.EnsureDirectory($"{projectDir}/{Path.GetDirectoryName(path)}");
            var text = decompiler.DecompileTypeAsString(type.FullTypeName);
            File.WriteAllText($"{projectDir}/{path}", text);
        }
        string ToFileName(ITypeDefinition type)
        {
            var typeParams = "";
            var namespacePart = "";
            if (!string.IsNullOrEmpty(type.Namespace))
            {
                namespacePart = type.Namespace + "\\";
            }
            if(type.TypeParameterCount > 0)
            {
                typeParams = $"_{type.TypeParameterCount}";
            }
            var relPath = $"{namespacePart}{type.Name}{typeParams}.cs";
            return relPath;
        }        
    }
}
