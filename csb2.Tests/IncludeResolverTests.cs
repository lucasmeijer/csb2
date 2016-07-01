using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiceIO;
using NUnit.Framework;

namespace csb2.Tests
{
    [TestFixture]
    public class IncludeResolverTests
    {
        private NPath _tmpDir;
        private IncludeParser _parser;

        [Test]
        public void CanParse()
        {
            var parser = new IncludeParser();
            var cpp = @"
#include ""GameObject.h""
    #include <SomeDir/SomeFile.h>
";

            CollectionAssert.AreEqual(new[] { "GameObject.h", "SomeDir/SomeFile.h"}, parser.Parse(cpp).ToArray());

        }

        [SetUp]
        public void Setup()
        {
            _tmpDir = NPath.SystemTemp.Combine("csbtests/parsertests").EnsureDirectoryExists();
            _tmpDir.Files().Delete();
            _parser = new IncludeParser();
        }

        [Test]
        public void CanResolve()
        {
            var myfile = MakeDummyFile("myfile.cpp");
            var myheader = MakeDummyFile("GameObject.h");

            Assert.AreEqual(myheader, _parser.Resolve("GameObject.h", myfile.Parent));
        }


        [Test]
        public void CanResolveInSubDir()
        {
            var myfile = MakeDummyFile("myfile.cpp");
            var myheader = MakeDummyFile("mydir/GameObject.h");

            Assert.AreEqual(myheader, _parser.Resolve("mydir/GameObject.h", myfile.Parent));
        }
        
        [Test]
        public void UnresolvableReturnsNull()
        {
            var myfile = MakeDummyFile("myfile.cpp");
            Assert.IsNull(_parser.Resolve("GameObject.h", myfile.Parent));
        }
        
        [Test]
        public void ResolveInIncludeDir()
        {
            var myfile = MakeDummyFile("myfile.cpp");
            var myheader = MakeDummyFile("mydir/GameObject.h");
            Assert.AreEqual(myheader, _parser.Resolve("GameObject.h", myfile.Parent, new[] { _tmpDir.Combine("mydir")}));
        }


        [Test]
        public void weird()
        {
            var a = new HashSet<NPath>();
            a.Add(new NPath("hello/there"));
            a.Add(new NPath("hello/there"));
            a.Add(new NPath("hello/there"));
            Assert.AreEqual(1, a.Count);
        }
        private NPath MakeDummyFile(string fileName)
        {
            var nPath = _tmpDir.Combine(fileName);
            nPath.EnsureParentDirectoryExists();
            return nPath.WriteAllText("//hello");
        }
    }
}
