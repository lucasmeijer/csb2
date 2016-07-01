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
    public class ProjectGenerator
    {
        [Test]
        public void MakeBigProjects()
        {
            var dir = new NPath("c:/test/projects/");
            dir.EnsureDirectoryExists();
            dir.Delete();
            dir.EnsureDirectoryExists();

            for (int i = 0; i != 5; i++)
            {
                MakeProject(dir.Combine("Project"+i), 30);    
            }
        }

        void MakeProject(NPath dir, int amountOfFiles)
        {
            dir.EnsureDirectoryExists();
            for (int i = 0; i != amountOfFiles; i++)
            {
                var contents = $@"
#include ""stdio.h""
#include <string>
#include <vector>
#include <set>

void SomeFunction{i}()
{{
  printf(""hello there sailor {i}\n"");
}}
";

                if (i == 0)
                    contents += @"
int main()
{
    return 0;
}
";


                dir.Combine($"File{i}.cpp").WriteAllText(contents);
            }
        }

    }
}
