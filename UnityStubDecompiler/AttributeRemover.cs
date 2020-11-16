using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.CSharp.Transforms;
using ICSharpCode.Decompiler.Semantics;
using System.Linq;

namespace UnityStubDecompiler
{
    public class AttributeRemover : DepthFirstAstVisitor, IAstTransform
    {
        public override void VisitAttributeSection(AttributeSection attributeSelection)
        {
            var attribute = attributeSelection.Attributes.First();
            var annotation = attribute.Annotation<MemberResolveResult>();
            if(annotation.Type.FullName == "System.SerializableAttribute" || annotation.Type.FullName == "UnityEngine.SerializeField")
            {
                return;
            }
            attributeSelection.Remove();
        }
        public void Run(AstNode rootNode, TransformContext context)
        {
            rootNode.AcceptVisitor(this);
        }
    }
}
