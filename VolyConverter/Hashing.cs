using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace VolyConverter
{
    public delegate string HashMethod(string filePath);

    public static class Hashing
    {
        private delegate HashAlgorithm Hasher();

        public static string HashFileMd5(string filePath)
        {
            return ComputeHash(filePath, MD5.Create);
        }

        public static string HashFileSha1(string filePath)
        {
            return ComputeHash(filePath, SHA1.Create);
        }

        public static string HashFileSha256(string filePath)
        {
            return ComputeHash(filePath, SHA256.Create);
        }

        private static string ComputeHash(string path, Hasher hasher)
        {
            using (HashAlgorithm method = hasher.Invoke())
            using (var stream = System.IO.File.OpenRead(path))
            {
                var h = method.ComputeHash(stream);
                return ConvertToHex(h);
            }
        }

        private static readonly uint[] hexLookup = CreateLookup();
        private static uint[] CreateLookup()
        {
            var result = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                string s = i.ToString("X2");
                result[i] = s[0] + ((uint)s[1] << 16);
            }
            return result;
        }

        private static string ConvertToHex(byte[] arr)
        {
            var result = new char[arr.Length * 2];
            for (int i = 0; i < arr.Length; i++)
            {
                var val = hexLookup[arr[i]];
                result[2 * i] = (char)val;
                result[2 * i + 1] = (char)(val >> 16);
            }
            return new string(result);
        }
    }
}
