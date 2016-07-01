using System;
using System.IO;
using System.Security.Cryptography;
using NiceIO;

namespace csb2
{
    internal class Hashing
    {
        public static UInt64 CalculateHash(string read)
        {
            UInt64 hashedValue = 3074457345618258791ul;
            for (int i = 0; i < read.Length; i++)
            {
                hashedValue += read[i];
                hashedValue *= 3074457345618258799ul;
            }
            return hashedValue;
        }

        public static string CalculateHash(NPath file)
        {
            using (FileStream stream = System.IO.File.OpenRead(file.ToString()))
            {
                var sha = new SHA256Managed();
                byte[] checksum = sha.ComputeHash(stream);
                return BitConverter.ToString(checksum).Replace("-", String.Empty);
            }
        }
    }
}