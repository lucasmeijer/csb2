using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NiceIO;
using ProtoBuf;
using ServiceStack;

namespace csb2
{
    public class PreviousBuildsDatabase
    {
        private readonly NPath _dbfile;
        private readonly ConcurrentDictionary<string, Entry> _entries = new ConcurrentDictionary<string, Entry>();

        public PreviousBuildsDatabase(NPath dbfile)
        {
            _dbfile = dbfile;
            if (dbfile != null && dbfile.FileExists())
            {
                using (var file = File.OpenRead(_dbfile.ToString()))
                {
                    var values = Serializer.Deserialize<EntryContainer>(file).entries;
                    foreach (var value in values)
                        _entries.TryAdd(value.File, value);
                }
            }
            Instance = this;
        }

        public static PreviousBuildsDatabase Instance { get; private set; }

        public void Save()
        {
            using (var file = File.Create(_dbfile.ToString()))
            {
                Serializer.Serialize(file, new EntryContainer() { entries = _entries.Values});
            }
        }

        [ProtoContract]
        public class EntryContainer
        {
            [ProtoMember(1)]
            public IEnumerable<Entry> entries;
        }

        [ProtoContract]
        public class Entry
        {
            [ProtoMember(1)]
            public string File;
            [ProtoMember(2)]
            public DateTime TimeStamp;
            [ProtoMember(3)]
            public InputsSumary InputsSummary;
        }
        
        public bool TryGetInfoFor(string file, out Entry result)
        {
            return _entries.TryGetValue(file, out result);
        }

        public void SetInfoFor(Entry entry)
        {
            _entries[entry.File] = entry;
        }
    }

    [ProtoContract]
    public class InputsSumary
    {
        [ProtoMember(1)]
        public string TargetFileName;
        [ProtoMember(2)]
        public string CommandLine;
        [ProtoMember(3)]
        public FileSummary[] Dependencies;

        public string Hash
        {
            get
            {
                var sb = new StringBuilder(TargetFileName);
                sb.Append(CommandLine);
                foreach (var dep in Dependencies)
                {
                    sb.Append(dep.FileName);
                    sb.Append(dep.Hash);
                }
                return sb.ToString();
            }
        }

        public bool Matches(InputsSumary newSummary, out string difference)
        {
            if (newSummary.TargetFileName != TargetFileName)
            {
                difference = $"New summary's TargetFileName is {newSummary.TargetFileName} but old one is {TargetFileName}";
                return false;
            }

            if (newSummary.CommandLine != CommandLine)
            {
                difference = $"New summary's CommandLine is {newSummary.CommandLine} but old one is {CommandLine}";
                return false;
            }

            if (newSummary.Dependencies.Length != Dependencies.Length)
            {
                difference = $"New summary has {newSummary.Dependencies.Length} dependencies, but old one has {Dependencies.Length}";
                difference += "\nNewDeps:\n";
                foreach (var dep in newSummary.Dependencies)
                    difference += dep.FileName + "\n";

                difference += "\n\nOldDeps:\n";
                foreach (var dep in Dependencies)
                    difference += dep.FileName + "\n";

                return false;
            }

            for (int i = 0; i != newSummary.Dependencies.Length; i++)
            {
                var old = Dependencies[i];
                var newer = newSummary.Dependencies[i];

                if (old.FileName != newer.FileName)
                {
                    difference = $"Dependency {i} in new summary has filename {newer.FileName}, but old one has {old.FileName}";
                    return false;
                }
                if (old.TimeStamp != newer.TimeStamp)
                {
                    difference = $"Dependency {newer.FileName} in new summary has timestamp {newer.TimeStamp}, but old one has {old.TimeStamp}";
                    return false;
                }
                if (old.Hash != newer.Hash)
                {
                    difference = $"Dependency {newer.FileName} in new summary has hash {newer.Hash}, but old one has {old.Hash}";
                    return false;
                }
            }
            difference = "";
            return true;
        }
    }

    [ProtoContract]
    public class FileSummary
    {
        [ProtoMember(1)]
        public string FileName;
        [ProtoMember(2)]
        public DateTime TimeStamp;
        [ProtoMember(3)]
        public string Hash;
    }
}