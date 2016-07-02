using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiceIO;
using NUnit.Framework.Internal;
using NUnit.Framework;

namespace csb2.Tests
{
    [TestFixture]
    public class JamOutputParserTests
    {
        [Test]
        public void Parse()
        {
            ParseAndAssert("/w123 /w234", "/w123", "/w234");
        }

        [Test]
        public void SpacesInQuoted()
        {
            ParseAndAssert(@"/I""hello sailor"" /I""johny""", @"/I""hello sailor""", @"/I""johny""");
        }

        [Test]
        public void CompletelyInQuotesGetsStripped()
        {
            ParseAndAssert(@"""/Ihello""", "/Ihello");
        }

        [Test]
        public void FindInvocationLinesInJamOutput()
        {
            var lines = new NPath("c:/unity2/jamoutput").ReadAllLines();

            var parser = new JamOutputParser();
            int found = 0;

            var cs = new StringBuilder();
            HashSet<string> processed = new HashSet<string>();
            cs.AppendLine("using System.Collections.Generic; using csb2; using NiceIO; public static class JamOutput { public static IEnumerable<ObjectNode> GetObjectNodes() { var objects = new List<ObjectNode>(); ");
            for (int i = 0; i != lines.Length; i++)
            {
                if (!lines[i].StartsWith("/T")) continue;

                var cpp = FindCpp(i, lines, parser);
                if (cpp == null)
                    continue;
                if (processed.Contains(cpp))
                    continue;
                if (cpp.Contains("videoInput"))
                    continue;
                processed.Add(cpp);

                var file = new NPath(cpp);
                var flags = parser.ParseCommandLine(lines[i]);

                var single = flags.Single(f => f.StartsWith("/Fo"));
                var substring = single.Substring(3);
                var outputPath = new NPath(parser.StripFullyQuoted(substring));
                var outputObjFile = outputPath.Combine(file.FileName).ChangeExtension(".obj");

              
                cs.AppendLine($"objects.Add(new ObjectNode(");
                cs.AppendLine($"\t\tnew SourceFileNode(new NPath(\"{file.ToString(SlashMode.Forward)}\")), ");
                cs.AppendLine($"\t\tnew NPath(\"{outputObjFile.ToString(SlashMode.Forward)}\"),");

                var includeDirs = flags.Where(f => f.StartsWith("/I")).Select(s3=>parser.StripFullyQuoted(s3.Substring(2)))/*.Where(s => !s.Contains("Program Files"))*/.Distinct().Select(s2 => new NPath(s2));
                cs.AppendLine("\t\tnew [] {");
                foreach (var includeDir in includeDirs)
                {
                    if (includeDir.ToString() == ".")
                        continue;
                    if (includeDir.ToString().Contains("dshow/include"))
                        continue;
                    cs.AppendLine($"\t\t\tnew NPath(\"{includeDir.ToString(SlashMode.Forward)}\"),");
                }
                cs.AppendLine("},");

                var defines = flags.Where(f => f.StartsWith("/D")).Distinct().Select(d => d.Substring(2));
                cs.AppendLine("\t\tnew [] {");
                foreach (var define in defines)
                {
                    cs.AppendLine($"\t\t\t\"{define}\",");
                }
                cs.AppendLine("},");
                
                var skipflags = new[] {"/D", "/I", "/Fo", "/Fd", "/c", "/Fp", "/Yc", "/Yl","/Yu","/DEBUG"};
                var restFlags = flags.Where(f => !skipflags.Any(f.StartsWith)).Distinct().Select(EscapeQuotes);
                
                cs.AppendLine("\t\tnew [] {");
                foreach (var restFlag in restFlags)
                {
                    cs.AppendLine($"\t\t\t\"{restFlag}\",");
                }
                cs.AppendLine("}));");


               // if (processed.Count > 50)
                 //   break;

            }
            cs.AppendLine("return objects;}}");

            new NPath("c:/users/lucas/desktop/csb2/csb2/JamOutput.cs").WriteAllText(cs.ToString());

        }

        private object EscapeQuotes(string arg)
        {
            return arg.Replace("\"", "\\\"");
        }

        private static string FindCpp(int i, string[] lines, JamOutputParser parser)
        {
            for (int searchBack = i; searchBack > 0 && searchBack > i - 10; searchBack--)
            {
                if (lines[searchBack].Contains(@"\vc\bin\amd64\cl"))
                {
                    return parser.ParseCommandLine(lines[searchBack]).Last();
                }
            }
            return null;
        }

        private static void ParseAndAssert(string input, params string[] expected)
        {
            var parser = new JamOutputParser();
            IEnumerable<string> result = parser.ParseCommandLine(input);
            CollectionAssert.AreEqual(expected, result);
        }
    }
}
