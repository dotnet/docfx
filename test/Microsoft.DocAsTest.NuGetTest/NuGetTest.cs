using System;
using System.IO;

namespace Microsoft.DocAsTest.NuGetTest
{
    public class NuGetTest
    {
        [MarkdownTest("data/**/*.md")]
        public void Foo(string filename)
        {
            File.WriteAllText(filename, "");
        }
    }
}
