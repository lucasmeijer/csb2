using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NiceIO;
using ProtoBuf;
using Unity.TinyProfiling;

namespace csb2
{
    class FileHashProvider
    {
        private readonly NPath _hashDatabase;
        public static FileHashProvider Instance { get; private set; }

        public FileHashProvider(NPath hashDatabase)
        {
            _hashDatabase = hashDatabase;
            Instance = this;
            if (!_hashDatabase.FileExists())
                return;
            using (TinyProfiler.Section("Load Hash DB"))
            using (var file = File.OpenRead(_hashDatabase.ToString()))
            {
                var values = Serializer.Deserialize<HashDatabase>(file).entries;
                foreach (var value in values)
                {
                    if (new NPath(value.Name).TimeStamp == value.TimeStamp)
                        _fileHashes.TryAdd(value.Name, value);
                }
            }

        }

        public void Save()
        {
            using (var file = File.Create(_hashDatabase.ToString()))
            {
                Serializer.Serialize(file, new HashDatabase() { entries = _fileHashes.Values });
            }
        }

        [ProtoContract]
        public class HashDatabase
        {
            [ProtoMember(1)]
            public IEnumerable<Entry> entries;
        }

        [ProtoContract]
        public class Entry
        {
            [ProtoMember(1)]
            public string Name;
            [ProtoMember(2)]
            public DateTime TimeStamp;
            [ProtoMember(3)]
            public string Hash;
        }


        readonly ConcurrentDictionary<string, Entry> _fileHashes = new ConcurrentDictionary<string, Entry>();

        public string HashFor(NPath file)
        {
            Entry result;
            if (_fileHashes.TryGetValue(file.ToString(), out result))
                return result.Hash;

            result = new Entry {Hash = Hashing.CalculateHash(file), TimeStamp = file.TimeStamp, Name = file.ToString()};
            _fileHashes[file.ToString()] = result;
            return result.Hash;
 ;       }
    }

    
    internal class Hashing
    {
        public static string CalculateHash(string read)
        {
            var md5 = new MD5CryptoServiceProvider();
            byte[] checksum = md5.ComputeHash(Encoding.UTF8.GetBytes(read));
            return BitConverter.ToString(checksum).Replace("-", String.Empty);
        }

        public static string CalculateHash(NPath file)
        {
            using (TinyProfiler.Section("CalculateHash: "+file))
            using (FileStream stream = File.OpenRead(file.ToString()))
            {
                var md5 = new MD5CryptoServiceProvider();
                byte[] checksum = md5.ComputeHash(stream);
                return BitConverter.ToString(checksum).Replace("-", String.Empty);
            }
        }
    }
}