using System;
using System.IO;

namespace UnityStubDecompiler
{
    public class FileUtil
    {
        public static void EnsureDirectory(string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
        public static string GetRelPath(string rootDir, string filePath)
        {
            if (!rootDir.EndsWith("/"))
            {
                rootDir += "/";
            }
            Uri fullPath = new Uri(filePath, UriKind.Absolute);
            Uri relRoot = new Uri(rootDir, UriKind.Absolute);

            string relPath = relRoot.MakeRelativeUri(fullPath).ToString();
            return relPath;
        }
    }
}
