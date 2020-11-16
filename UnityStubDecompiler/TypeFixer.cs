using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.CSharp.Transforms;
using ICSharpCode.Decompiler.TypeSystem;
using System.Collections.Generic;
using System.Linq;

namespace UnityStubDecompiler
{
    public class TypeFixer : DepthFirstAstVisitor, IAstTransform
    {
        StubDecompiler Decompiler;
        public TypeFixer(StubDecompiler stubDecompiler)
        {
            Decompiler = stubDecompiler;
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
            foreach (var subclass in typeDeclaration.Children.OfType<TypeDeclaration>())
            {
                var resolve = subclass.GetResolveResult();
                if (!Decompiler.HasType(resolve.Type.GetDefinition()))
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
                if (member is MethodDeclaration)
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
                    var def = typeDeclaration.GetResolveResult().Type.GetDefinition();
                    var type = Decompiler.GetType(def);
                    var fields = type.Fields;
                    if (!fields.Any(f => f.Name == fd.GetSymbol().Name))
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
}
