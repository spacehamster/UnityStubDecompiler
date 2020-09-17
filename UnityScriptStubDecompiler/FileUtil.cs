using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Decompile
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
