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
            //TODO: Why doesn't this detect SerializeField
            var attributeText = attribute.ToString().Trim();
            if (attributeText == "[Serializable]" || attributeText == "[SerializeField]") return;
            attribute.Remove();
        }
        public void Run(AstNode rootNode, TransformContext context)
        {
            rootNode.AcceptVisitor(this);
        }
    }
}
