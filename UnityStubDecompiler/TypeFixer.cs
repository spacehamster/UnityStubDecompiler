﻿using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.CSharp.Transforms;
using ICSharpCode.Decompiler.TypeSystem;
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
            var typeDefinition = typeDeclaration.GetResolveResult().Type.GetDefinition();
            var type = Decompiler.GetType(typeDefinition);
            foreach (var member in typeDeclaration.Members)
            {
                if (member is MethodDeclaration md)
                {
                    var methods = type.Methods;
                    if (!methods.Any(f => f.Name == md.GetSymbol().Name))
                    {
                        member.Remove();
                    }
                    else
                    {
                        foreach(var child in md.Body.Children)
                        {
                            child.Remove();
                        }
                        if (md.ReturnType.ToString() != "void")
                        {
                            var simpleType = new SimpleType("System.NotImplementedException");
                            var objCreateExpression = new ObjectCreateExpression(simpleType);
                            var throwStatement = new ThrowStatement(objCreateExpression);
                            md.Body.Add(throwStatement);
                        }
                    }
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
                else if (member is DestructorDeclaration)
                {
                    member.Remove();
                }
                else if (member is FieldDeclaration fd)
                {
                    var fields = type.Fields;
                    if (!fields.Any(f => f.Name == fd.GetSymbol().Name))
                    {
                        member.Remove();
                    }
                }
                else if (member is TypeDeclaration)
                {

                }
                else if (member is EnumMemberDeclaration)
                {

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
