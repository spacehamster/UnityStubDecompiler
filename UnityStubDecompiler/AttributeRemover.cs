using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.CSharp.Transforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnityStubDecompiler
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
}
