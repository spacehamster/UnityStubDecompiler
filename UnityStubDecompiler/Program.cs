using System.IO;

namespace UnityStubDecompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();
            options.GenerateSolution = true;
            options.SolutionDirectoryName = Path.GetFileNameWithoutExtension(Path.GetDirectoryName(args[0]));
            StubDecompiler.DecompileProject(args[0], options);
        }
    }
}
