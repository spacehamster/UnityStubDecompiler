using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;
using System.Collections.Generic;

namespace UnityStubDecompiler
{
    public class DecompileModule
    {
        public readonly IModule Module;
        public readonly CSharpDecompiler Decompiler;
        private HashSet<IModule> m_References = new HashSet<IModule>();
        public IReadOnlyCollection<IModule> References => m_References;
        public DecompileModule(IModule module, CSharpDecompiler decompiler)
        {
            Module = module;
            Decompiler = decompiler;
        }
        public void AddReference(IModule module)
        {
            m_References.Add(module);
        }
    }
}
