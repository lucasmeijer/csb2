﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NiceIO;
using ProtoBuf;

namespace csb2
{
    public class PreviousBuildsDatabase
    {
        private readonly NPath _dbfile;
        private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>();

        public PreviousBuildsDatabase(NPath dbfile)
        {
            _dbfile = dbfile;
            if (dbfile != null && dbfile.FileExists())
            {
                using (var file = File.OpenRead(_dbfile.ToString()))
                {
                    var values = Serializer.Deserialize<EntryContainer>(file);
                    _entries = values.entries.ToDictionary(e => e.Name, e => e);
                }
            }
        }

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
            public string Name;
            [ProtoMember(2)]
            public DateTime TimeStamp;
        }

        public bool TryGetInfoFor(string file, out Entry result)
        {
            return _entries.TryGetValue(file, out result);
        }

        public void SetInfoFor(Entry entry)
        {
            _entries[entry.Name] = entry;
        }
    }
}