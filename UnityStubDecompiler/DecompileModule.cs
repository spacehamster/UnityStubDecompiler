using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;

namespace UnityStubDecompiler
{
    public class DecompileModule
    {
        public readonly IModule Module;
        public readonly CSharpDecompiler Decompiler;
        public DecompileModule(IModule module, CSharpDecompiler decompiler)
        {
            Module = module;
            Decompiler = decompiler;
        }
    }
}
