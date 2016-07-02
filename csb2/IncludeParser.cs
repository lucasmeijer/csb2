using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NiceIO;
using Unity.TinyProfiling;

namespace csb2
{
    public class IncludeParser
    {
        readonly Regex _regex = new Regex("^[ 	]*#[ 	]*include[ 	]*[<\"](?<file>[^\">]*)[\">].*$", RegexOptions.Multiline) ;

        private readonly ConcurrentDictionary<string, string[]> _fileToHeaderNamesCache = new ConcurrentDictionary<string, string[]>();


        public IEnumerable<string> Parse(NPath file)
        {
            string[] result;
            if (_fileToHeaderNamesCache.TryGetValue(file.ToString(), out result))
                return result;

            using (TinyProfiler.Section("ReadAndParse " + file))
            {
                result = Parse(file.ReadAllText()).ToArray();
                _fileToHeaderNamesCache[file.ToString()] = result;
            }
            return result;
        }

        public IEnumerable<string> Parse(string cpp)
        {
            foreach (Match match in _regex.Matches(cpp))
                yield return match.Groups["file"].Value;
        }

        public NPath Resolve(string headerName, NPath myfile, IEnumerable<NPath> includeDirectories = null )
        {
            var sibbling = myfile.Combine(headerName);
            if (sibbling.FileExists())
                return sibbling;
            if (includeDirectories == null)
                return null;
            foreach (var includeDir in includeDirectories)
            {
                var n = includeDir.Combine(headerName);
                if (n.FileExists())
                    return n;
            }
            return null;
        }

        private readonly ConcurrentDictionary<string, NPath[]> _includedFilesCache = new ConcurrentDictionary<string, NPath[]>();

        public IEnumerable<NPath> FindIncludedFiles(NPath file, IEnumerable<NPath> includeDirectories, IEnumerable<NPath> toolChainIncludeDirectories, HashSet<NPath> alreadyProcessed = null)
        {
            var key = new StringBuilder(file.ToString());
            foreach (var i in includeDirectories)
                key.Append(i);

            NPath[] result;
            var keyString = key.ToString();
            if (_includedFilesCache.TryGetValue(keyString, out result))
                return result;

            if (alreadyProcessed==null)
                alreadyProcessed = new HashSet<NPath>();

            var headerNames = Parse(file);

            var files = new List<NPath>();
            var compiledIncludeDirs = includeDirectories.Concat(toolChainIncludeDirectories).ToArray();
            
            foreach (var headerName in headerNames)
            {
                var includedFile = Resolve(headerName, file.Parent, compiledIncludeDirs);
                if (includedFile == null)
                    continue;
                
                //not scan systeam headers as an optimization
                if (!includedFile.IsRelative && toolChainIncludeDirectories.Any(i => includedFile.IsChildOf(i)) || includedFile.ToString().Contains("Program Files"))
                    continue;

                //           throw new ArgumentException("Unable to resolve: " + headerName + " for " + _cppFile.File);
                if (alreadyProcessed.Contains(includedFile))
                    continue;
                alreadyProcessed.Add(includedFile);
                files.Add(includedFile);

                files.AddRange(FindIncludedFiles(includedFile, includeDirectories, toolChainIncludeDirectories, alreadyProcessed));
            }

            _includedFilesCache[keyString] = files.ToArray();
            return files;
        }
    }
}
