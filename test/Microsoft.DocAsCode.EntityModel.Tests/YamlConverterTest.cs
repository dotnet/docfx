namespace Microsoft.DocAsCode.EntityModel.Tests
{
    using Microsoft.DocAsCode.EntityModel.YamlConverters;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    [Trait("Owner", "zhyan")]
    [Trait("EntityType", "YamlConverter")]
    public class YamlConverterTest
    {
        [Fact]
        public void TestRelativePathRewriter()
        {
            var sr = new StringReader(@"- relative path link test
- external link [external link](http://www.microsoft.com).
- relative path [c](a/b/c.xml ""this is relative path"").
- name: x
  summary: relative path [a](../../a.xml).
- absolute path [a](/a.xml).
");
            var obj = YamlUtility.Deserialize<object>(sr);
            Assert.IsType<List<object>>(obj);
            var list = (List<object>)RelativePathRewriter.Rewrite(
                obj, (RelativePath)"x/y/z.yaml", (RelativePath)"api/z.yaml");
            Assert.Equal(5, list.Count);
            Assert.Equal("relative path link test", list[0]);
            Assert.Equal("external link [external link](http://www.microsoft.com).", list[1]);
            Assert.Equal(@"relative path [c](a/b/c.xml ""this is relative path"").", list[2]);
            {
                Assert.IsType<Dictionary<object, object>>(list[3]);
                var d = (Dictionary<object, object>)list[3];
                Assert.Equal(2, d.Count);
                Assert.Equal("x", d["name"]);
                Assert.Equal("relative path [a](../../a.xml).", d["summary"]);
            }
            Assert.Equal("absolute path [a](/a.xml).", list[4]);
        }
    }
}
