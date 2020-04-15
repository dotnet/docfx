// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Xunit;

namespace Microsoft.Docs.Build
{
    public class HtmlReaderTest
    {
        [Theory]

        // Comment
        [InlineData("", "")]
        [InlineData("<!--", "Comment:<!--")]
        [InlineData("<!---", "Comment:<!---")]
        [InlineData("<!----", "Comment:<!----")]
        [InlineData("<!---->", "Comment:<!---->")]
        [InlineData("<!----!", "Comment:<!----!")]
        [InlineData("<!----!-", "Comment:<!----!-")]
        [InlineData("<!----!->", "Comment:<!----!->")]
        [InlineData("<!----!--", "Comment:<!----!--")]
        [InlineData("<!----!-->", "Comment:<!----!-->")]
        [InlineData("<!--<!---->", "Comment:<!--<!---->")]
        [InlineData("<!-----", "Comment:<!-----")]
        [InlineData("<?DOCTYPE>", "Comment:<?DOCTYPE>")]
        [InlineData("<?>", "Comment:<?>")]
        [InlineData("<a", "Comment:<a")]
        [InlineData("<a ", "Comment:<a ")]
        [InlineData("<a/", "Comment:<a/")]
        [InlineData("<a b='cd>", "Comment:<a b='cd>")]
        [InlineData("<a b=\"cd>", "Comment:<a b=\"cd>")]

        // StartTag
        [InlineData("<a>", "StartTag:a")]
        [InlineData("<a >", "StartTag:a")]
        [InlineData("<a <>", "StartTag:a(<:[<])")]
        [InlineData("<a < >", "StartTag:a(<:[<])")]
        [InlineData("<a <a>", "StartTag:a(<a:[<a])")]
        [InlineData("<a<a>", "StartTag:a<a")]
        [InlineData("<a/>", "StartTag:a")]
        [InlineData("<a />", "StartTag:a")]
        [InlineData("<a     />", "StartTag:a")]
        [InlineData("<a/ >", "StartTag:a")]
        [InlineData("<a / >", "StartTag:a")]
        [InlineData("<abc def name='s' key=\"v\" />", "StartTag:abc(def:[def], name:s[name='s'], key:v[key=\"v\"])")]
        [InlineData("<a/b>", "StartTag:a(b:[b])")]

        // EndTag
        [InlineData("</a>", "EndTag:a")]
        [InlineData("</a >", "EndTag:a")]
        [InlineData("</a / >", "EndTag:a")]
        [InlineData("</a b name='c' >", "EndTag:a")]

        // Text
        [InlineData(" ", "Text: ")]
        [InlineData("a", "Text:a")]
        [InlineData(">", "Text:>")]
        [InlineData("<>", "Text:<>")]
        [InlineData("< /a>", "Text:< /a>")]
        [InlineData("< /a >", "Text:< /a >")]
        [InlineData("< a >", "Text:< a >")]
        [InlineData("< a/ >", "Text:< a/ >")]
        [InlineData("< a />", "Text:< a />")]

        // Attributes
        [InlineData("<a b=c>", "StartTag:a(b:c[b=c])")]
        [InlineData("<a href='single quote'>", "StartTag:a(href:single quote[href='single quote'])")]
        [InlineData("<a href=\"double quote\">", "StartTag:a(href:double quote[href=\"double quote\"])")]
        [InlineData("<a href=noquote>", "StartTag:a(href:noquote[href=noquote])")]
        [InlineData("<a data-bool>", "StartTag:a(data-bool:[data-bool])")]
        [InlineData("<a =b>", "StartTag:a(=b:[=b])")]
        [InlineData("<a  b= c>", "StartTag:a(b:c[b= c])")]
        [InlineData("<asdf b = cdef f  >", "StartTag:asdf(b:cdef[b = cdef], f:[f])")]
        [InlineData("<a b =c/>", "StartTag:a(b:c/[b =c/])")]
        [InlineData("<a b =c />", "StartTag:a(b:c[b =c])")]
        [InlineData("<a b =c / />", "StartTag:a(b:c[b =c])")]
        [InlineData("<a b =c < />", "StartTag:a(b:c[b =c], <:[<])")]
        [InlineData("<a b ='c <' />", "StartTag:a(b:c <[b ='c <'])")]
        [InlineData("<a b =< />", "StartTag:a(b:<[b =<])")]
        [InlineData("<a < = b />", "StartTag:a(<:b[< = b])")]
        [InlineData("<a b/c=d />", "StartTag:a(b:[b], c:d[c=d])")]
        [InlineData("<a b=>", "StartTag:a(b:[b=])")]

        // Composite
        [InlineData("<a></a>", "StartTag:a, EndTag:a")]
        [InlineData("<a><a>", "StartTag:a, StartTag:a")]
        [InlineData("<a>b</a>", "StartTag:a, Text:b, EndTag:a")]
        [InlineData("<!--<!---->-->", "Comment:<!--<!---->, Text:-->")]
        [InlineData("<!--a--><b>", "Comment:<!--a-->, StartTag:b")]
        [InlineData("<!--a--> <b>", "Comment:<!--a-->, Text: , StartTag:b")]
        [InlineData("<!--a-->   </b>", "Comment:<!--a-->, Text:   , EndTag:b")]
        [InlineData("a < b <c>", "Text:a , Text:< b , StartTag:c")]
        [InlineData("<a b='cd>'></a>", "StartTag:a(b:cd>[b='cd>']), EndTag:a")]

        // Parsing errors https://html.spec.whatwg.org/multipage/parsing.html#parse-errors
        // CDATA, DOCTYPE and script tag specialties are ignored
        [InlineData("<!-->", "Comment:<!-->")]
        [InlineData("<!--->", "Comment:<!--->")]
        [InlineData("&#qux;", "Text:&#qux;")]
        [InlineData("中文", "Text:中文")]
        [InlineData("</a href='url'>", "EndTag:a")]
        [InlineData("<", "Text:<")]
        [InlineData("</", "Text:</")]
        [InlineData("<div id=", "Comment:<div id=")]
        [InlineData("<!-- a --!>", "Comment:<!-- a --!>")]
        [InlineData("<!a>", "Comment:<!a>")]
        [InlineData("<42></42>", "Text:<42>, Comment:</42>")]
        [InlineData("</>", "EndTag:")]
        [InlineData("&not;in", "Text:&not;in")]
        [InlineData("<div id=\"foo\"class=\"bar\">", "StartTag:div(id:foo[id=\"foo\"], class:bar[class=\"bar\"])")]
        [InlineData("<!-- <!-- nested --> -->", "Comment:<!-- <!-- nested -->, Text: -->")]
        [InlineData("<div/><span></span><span></span>", "StartTag:div, StartTag:span, EndTag:span, StartTag:span, EndTag:span")]
        [InlineData("<div foo<div>", "StartTag:div(foo<div:[foo<div])")]
        [InlineData("<div id'bar'>", "StartTag:div(id'bar':[id'bar'])")]
        [InlineData("<div foo=b'ar'>", "StartTag:div(foo:b'ar'[foo=b'ar'])")]
        [InlineData("<div foo=\"bar\" =\"baz\">", "StartTag:div(foo:bar[foo=\"bar\"], =\"baz\":[=\"baz\"])")]
        [InlineData("<?xml-stylesheet type=\"text/css\" href=\"style.css\"?>", "Comment:<?xml-stylesheet type=\"text/css\" href=\"style.css\"?>")]
        [InlineData("<div / id=\"foo\">", "StartTag:div(id:foo[id=\"foo\"])")]
        public void ReadHtml(string html, string expected)
        {
            var actual = new List<string>();
            var reader = new HtmlReader(html);

            while (reader.Read())
            {
                var content = reader.Type == HtmlTokenType.StartTag || reader.Type == HtmlTokenType.EndTag
                    ? reader.Name.ToString() : reader.Token.ToString();

                if (reader.Attributes.Length > 0)
                {
                    var attributes = new List<string>();
                    foreach (var attribute in reader.Attributes)
                    {
                        attributes.Add($"{attribute.Name.ToString()}:{attribute.Value.ToString()}[{attribute.Token.ToString()}]");
                    }
                    content += $"({string.Join(", ", attributes)})";
                }

                actual.Add($"{reader.Type}:{content}");
            }

            Assert.Equal(expected, string.Join(", ", actual));
        }
    }
}
