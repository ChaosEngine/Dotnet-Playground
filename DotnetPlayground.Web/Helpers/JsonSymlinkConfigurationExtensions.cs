using Microsoft.Extensions.FileProviders;
using Mono.Unix;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Extensions.Configuration
{
    internal static class JsonSymlinkConfigurationExtensions
    {
        internal static IConfigurationBuilder AddSymLinkJsonFile(this IConfigurationBuilder c, string relativePath, bool optional, bool reloadOnChange)
        {
            var fileInfo = c.GetFileProvider().GetFileInfo(relativePath);

            if (TryGetSymLinkTarget(fileInfo.PhysicalPath, out string targetPath))
            {
                string targetDirectory = Path.GetDirectoryName(targetPath);

                if (TryGetSymLinkTarget(targetDirectory, out string symlinkDirectory))
                {
                    targetDirectory = symlinkDirectory;
                }

                c.AddJsonFile(new PhysicalFileProvider(targetDirectory), Path.GetFileName(targetPath), optional, reloadOnChange);
            }
            else
            {
                c.AddJsonFile(relativePath, optional, reloadOnChange);
            }
			return c;
        }

        private static bool TryGetSymLinkTarget(string path, out string target, int maximumSymlinkDepth = 32)
        {
            target = null;

            int depth = 0;

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var symbolicLinkInfo = new UnixSymbolicLinkInfo(path);
                while (symbolicLinkInfo.Exists && symbolicLinkInfo.IsSymbolicLink)
                {
                    target = symbolicLinkInfo.ContentsPath;

                    if (!Path.IsPathFullyQualified(target))
                    {
                        target = Path.GetFullPath(target, Path.GetDirectoryName(symbolicLinkInfo.FullName));
                    }

                    symbolicLinkInfo = new UnixSymbolicLinkInfo(target);

                    if (depth++ > maximumSymlinkDepth)
                    {
                        throw new InvalidOperationException("Exceeded maximum symlink depth");
                    }
                }
            }
            return target != null;
        }
    }
}