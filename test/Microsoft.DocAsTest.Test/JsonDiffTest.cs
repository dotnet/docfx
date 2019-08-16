using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.DocAsTest
{
    public class JsonDiffTest
    {
        [Theory]

        // none
        [InlineData("{}", "{}", "")]

        [InlineData("{'a': null}", "{'a': null}", @"")]
        [InlineData("{'a': null}", "{}", @"
-  'a': null")]

        [InlineData("{'a': undefined}", "{}", "")]
        [InlineData("{'a': undefined}", "{'a': null}", @"
+  'a': null")]

        [InlineData("1", "2", @"
-1
+2")]
        [InlineData("[]", "2", @"
-[]
+2")]
        [InlineData("[1]", "[1,2]", @"
-  1
+  1,
+  2")]

        [InlineData("{'a': 1, 'b': 2}", "{'b': 2, 'a': 1}", "")]
        [InlineData("{'a': 1, 'b': { 'c': 2 }}", "{'a': 1, 'b': { 'c': 2 }}", "")]
        [InlineData("{'a': 1, 'b': { 'c': 2 }}", "{'a': 1, 'b': { 'c': 3 }}", @"
-    'c': 2
+    'c': 3")]
        [InlineData("{'a': 1}", "{'a': 1, 'b': 2}", "")]

        // negate
        [InlineData("'!a'", "'b'", "")]
        [InlineData("'!a'", "'a'", @"
-'!a'
+'a'")]

        // ignore null
        [InlineData("{'ignore-null': null}", "{'ignore-null': 'a'}", "")]
        [InlineData("{'ignore-null': null}", "{'ignore-null': true}", "")]
        [InlineData("{'ignore-null': null}", "{}", @"
-  'ignore-null': null")]

        // wildcard
        [InlineData("'a*b'", "'a123b'", "")]
        [InlineData("'a*b'", "'ab'", "")]
        [InlineData("'a*b'", "'abc'", @"
-'a*b'
+'abc'")]
        [InlineData("'a*b'", "'a*b'", "")]
        [InlineData("'ab'", "'a*b'", @"
-'ab'
+'a*b'")]
        [InlineData("[1,'*']", "[1,'2']", "")]
        [InlineData("[1,'*',2]", "[1,'2',3]", @"
-  2
+  3")]

        // regex
        [InlineData("'/.*/'", "''", "")]
        [InlineData("'/.*/'", "'a'", "")]
        [InlineData("'/^.?$/'", "'a'", "")]
        [InlineData("'/^.?$/'", "'aa'", @"
-'/^.?$/'
+'aa'")]

        // jsonfile
        [InlineData("{'a.json': 'true'}", "{'a.json': 'true'}", "")]

        [InlineData(@"{'a.json': '{\'a\':undefined}'}", @"{'a.json': '{}'}", "")]
        [InlineData(@"{'a.json': '{\'a\':undefined}'}", @"{'a.json': '{\'a\':null}'}", @"
+  \'a\': null")]

        [InlineData(@"{'a/b.json': '{\'a\':1}'}", @"{'a/b.json': '{\'a\':1}'}", "")]
        [InlineData(@"{'a/b.json': '{\'a\':1}'}", @"{'a/b.json': '{\'a\':1, \'b\':2}'}", "")]
        [InlineData(@"{'a/b.json': '{\'a\':1}'}", @"{'a/b.json': '{\'a\':2}'}", @"
-  \'a\': 1
+  \'a\': 2")]

        [InlineData(@"{'a.json': '{\'a\':\'*\'}'}", @"{'a.json': '{\'a\':\'anything\'}'}", "")]

        // htmlfile
        [InlineData("{'a.html': 'a'}", "{'a.html': 'b'}", @"
-  'a.html': 'a'
+  'a.html': 'b'")]
        [InlineData("{'a.html': '<p> a </p>'}", "{'a.html': '<p>a</p>'}", "")]
        [InlineData("{'a.html': '<!-- a -->'}", "{'a.html': '<!-- b -->'}", "")]
        [InlineData(@"{'a.html': '<div a=\'1\' b= \'2\'></div>'}", @"{'a.html': '<div b=\'2\' a =\'1\'></div>'}", "")]
        public static void Run(string expected, string actual, string diff)
        {
            var jsonDiff = new JsonDiffBuilder()
                .UseAdditionalProperties()
                .UseIgnoreNull((e, a, name) => name == "ignore-null")
                .UseNegate()
                .UseRegex()
                .UseWildcard()
                .UseJson()
                .UseHtml()
                .Build();

            var actualDiff = jsonDiff.Diff(
                    JToken.Parse(expected.Replace('\'', '\"')),
                    JToken.Parse(actual.Replace('\'', '\"')));

            var actualChanges = actualDiff
                .Replace("\r", "")
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Where(line => line.StartsWith("+") || line.StartsWith("-"));

            jsonDiff.Verify(
                diff.Trim().Replace("\r", "").Replace('\'', '\"'),
                string.Join('\n', actualChanges));
        }
    }
}
