using System;
using System.Collections.Generic;
using System.IO;
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
            return ComputeFileHash(filePath, MD5.Create);
        }

        public static string HashFileSha1(string filePath)
        {
            return ComputeFileHash(filePath, SHA1.Create);
        }

        public static string HashFileSha256(string filePath)
        {
            return ComputeFileHash(filePath, SHA256.Create);
        }

        private static string ComputeFileHash(string path, Hasher hasher)
        {
            using (var stream = File.OpenRead(path))
            {
                return ComputeStreamHash(stream, hasher);
            }
        }

        private static string ComputeStreamHash(Stream s, Hasher hasher)
        {
            using (HashAlgorithm method = hasher.Invoke())
            {
                var h = method.ComputeHash(s);
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
