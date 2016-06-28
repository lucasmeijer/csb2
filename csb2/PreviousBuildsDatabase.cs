using System;

namespace csb2
{
    public class PreviousBuildsDatabase
    {
        public class Entry
        {
            public DateTime TimeStamp;
            public string Hash;
            public byte[] Inputs_hash;
        }

        public bool TryGetInfoFor(string file, out Entry result)
        {
            result = null;
            return false;
        }

        public void SetInfoFor(string name, Entry entry)
        {
        }
    }
}