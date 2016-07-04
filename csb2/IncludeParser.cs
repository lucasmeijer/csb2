using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Configuration;
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

        public IEnumerable<NPath> FindIncludedFiles(NPath file, NPath[] includeDirectories)
        {
            var key = new StringBuilder(file.ToString());
            foreach (var i in includeDirectories)
                key.Append(i);
            var cacheKey = key.ToString();


            NPath[] result;
            if (_includedFilesCache.TryGetValue(cacheKey, out result))
                return result;

            var results = new List<NPath>();
            var toProcess = new Queue<NPath>();
            toProcess.Enqueue(file);
            results.Add(file);
            
            while (toProcess.Any())
            {
                var f = toProcess.Dequeue();
                
                var directlyIncludedFiles = FindDirectlyIncludedFiles(f, includeDirectories, cacheKey);
                foreach(var includedFile in directlyIncludedFiles)
                    if (!results.Contains(includedFile))
                    {
                        toProcess.Enqueue(includedFile);
                        results.Add(includedFile);
                    }
            }

            var resultsArray = results.ToArray();
            _includedFilesCache[cacheKey] = resultsArray;

            return resultsArray;
        }

        private readonly ConcurrentDictionary<string, NPath[]> _directlyIncludedFilesCache = new ConcurrentDictionary<string, NPath[]>();

        NPath[] FindDirectlyIncludedFiles(NPath file, NPath[] includeDirectories, string cacheKey)
        {
            NPath[] result;
            if (_directlyIncludedFilesCache.TryGetValue(cacheKey, out result))
                return result;

            var headerNames = Parse(file);

            result = headerNames.Select(headerName => Resolve(headerName, file.Parent, includeDirectories)).Where(includedFile => includedFile != null && !includedFile.ToString().Contains("Program Files")).ToArray();
            _directlyIncludedFilesCache.TryAdd(cacheKey, result);
            return result;
        }
    }
}
