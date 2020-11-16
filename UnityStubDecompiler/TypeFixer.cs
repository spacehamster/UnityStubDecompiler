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
            foreach (var constraint in typeDeclaration.Constraints)
            {
                constraint.Remove();
            }
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
                else if (member is PropertyDeclaration)
                {
                    member.Remove();
                }
                else if (member is OperatorDeclaration)
                {
                    member.Remove();
                }
                else if (member is IndexerDeclaration)
                {
                    member.Remove();
                }
                else if (member is ConstructorDeclaration)
                {
                    member.Remove();
                }
                else if(member is CustomEventDeclaration)
                {
                    member.Remove();
                }
                else if (member is DelegateDeclaration)
                {
                    member.Remove();
                }
                else if(member is TypeDeclaration)
                {

                }
                else if (member is FieldDeclaration fd)
                {
                    var def = typeDeclaration.GetResolveResult().Type.GetDefinition();
                    var type = Decompiler.GetType(def);
                    var fields = type.Fields;
                    if (!fields.Any(f => f.Name == fd.GetSymbol().Name))
                    {
                        member.Remove();
                    }
                }
                else
                {

                }
            }
        }

        public void Run(AstNode rootNode, TransformContext context)
        {
            rootNode.AcceptVisitor(this);
        }
    }
}
