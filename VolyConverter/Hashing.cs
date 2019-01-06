using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VolyConverter
{
    internal static class Hashing
    {
        public static string HashFileMd5(string filePath)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            using (var stream = System.IO.File.OpenRead(filePath))
            {
                var h = md5.ComputeHash(stream);
                return BitConverter.ToString(h).Replace("-", string.Empty).ToLowerInvariant();
            }
        }
    }
}
