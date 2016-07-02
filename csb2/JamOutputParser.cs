using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csb2
{
    public class JamOutputParser
    {
        public IEnumerable<string> ParseCommandLine(string s)
        {
            var result = new List<string>();
            var sb = new StringBuilder();
            bool inquotes = false;
            for (int i = 0; i != s.Length; i++)
            {
                if (s[i] == ' ' && !inquotes)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                    continue;
                }

                if (s[i] == '\"')
                    inquotes = !inquotes;

                sb.Append(s[i]);  
            }
            result.Add(sb.ToString());
            return result.Select(StripFullyQuoted);
        }

        public string StripFullyQuoted(string arg)
        {
            if (arg[0] == '"' && arg[arg.Length - 1] == '"')
                return arg.Substring(1, arg.Length - 2);
            return arg;
        }
    }
}
