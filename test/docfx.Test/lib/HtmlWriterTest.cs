// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.Docs.Build
{
    public class HtmlWriterTest
    {
        [Theory]
        [InlineData("<a></a>", 0, "</a>")]
        [InlineData("<a></a>", 1, "<a>")]
        [InlineData("<a>b</a>", 1, "<a></a>")]
        [InlineData("<a>b</a>", 2, "<a>b")]
        public void HtmlWriter_RemoveToken(string html, int index, string expected)
        {
            var i = 0;
            var reader = new HtmlReader(html);
            var writer = new HtmlWriter(html.Length);

            while (reader.Read())
            {
                if (i++ == index)
                {
                    writer.RemoveToken();
                }
                writer.Write(reader);
            }

            Assert.Equal(expected, writer.ToString());
        }

        [Theory]
        [InlineData("<a></a>", 0, "x", "x</a>")]
        [InlineData("<a>b</a>", 1, "x", "<a>x</a>")]
        public void HtmlWriter_ReplaceToken(string html, int index, string replacement, string expected)
        {
            var i = 0;
            var reader = new HtmlReader(html);
            var writer = new HtmlWriter(html.Length);

            while (reader.Read())
            {
                if (i++ == index)
                {
                    writer.ReplaceToken(replacement);
                }
                writer.Write(reader);
            }

            Assert.Equal(expected, writer.ToString());
        }

        [Theory]
        [InlineData("<a/>", 0, "")]
        [InlineData("<a></a>", 0, "")]
        [InlineData("<a></a>", 1, "<a></a>")]
        [InlineData("<a>b</a>", 1, "<a>b</a>")]
        [InlineData("<a>b</a>", 0, "")]
        [InlineData("<a>b</a><c>", 0, "<c>")]
        [InlineData("<a><a/></a>", 1, "<a></a>")]
        [InlineData("<a><b><c>d</c></b>2</a>", 1, "<a>2</a>")]
        public void HtmlWriter_RemoveTag(string html, int index, string expected)
        {
            var i = 0;
            var reader = new HtmlReader(html);
            var writer = new HtmlWriter(html.Length);

            while (reader.Read())
            {
                if (i++ == index)
                {
                    writer.RemoveTag();
                }
                writer.Write(reader);
            }

            Assert.Equal(expected, writer.ToString());
        }

        [Theory]
        [InlineData("<a/>", 0, "x", "x")]
        [InlineData("<a><b><c>d</c></b>2</a>", 1, "x", "<a>x2</a>")]
        public void HtmlWriter_ReplaceTag(string html, int index, string replacement, string expected)
        {
            var i = 0;
            var reader = new HtmlReader(html);
            var writer = new HtmlWriter(html.Length);

            while (reader.Read())
            {
                if (i++ == index)
                {
                    writer.ReplaceTag(replacement);
                }
                writer.Write(reader);
            }

            Assert.Equal(expected, writer.ToString());
        }

        [Theory]
        [InlineData("<a bool></a>", 0, "x", "<a x></a>")]
        [InlineData("<a bool src='a.md'>b</a>", 1, "x", "<a bool x>b</a>")]
        public void HtmlWriter_ReplaceAttribute(string html, int index, string replacement, string expected)
        {
            var i = 0;
            var reader = new HtmlReader(html);
            var writer = new HtmlWriter(html.Length);

            while (reader.Read())
            {
                foreach (ref readonly var attribute in reader.Attributes)
                {
                    if (i++ == index)
                    {
                        writer.ReplaceAttribute(attribute, replacement);
                    }
                }
                writer.Write(reader);
            }

            Assert.Equal(expected, writer.ToString());
        }

        [Theory]
        [InlineData("<a bool></a>", 0, "<a ></a>")]
        [InlineData("<a bool src='a.md'>b</a>", 1, "<a bool >b</a>")]
        public void HtmlWriter_RemoveAttribute(string html, int index, string expected)
        {
            var i = 0;
            var reader = new HtmlReader(html);
            var writer = new HtmlWriter(html.Length);

            while (reader.Read())
            {
                foreach (ref readonly var attribute in reader.Attributes)
                {
                    if (i++ == index)
                    {
                        writer.RemoveAttribute(attribute);
                    }
                }
                writer.Write(reader);
            }

            Assert.Equal(expected, writer.ToString());
        }

        [Theory]
        [InlineData("<a bool></a>", 0, "x", "<a bool></a>")]
        [InlineData("<a bool src='a.md'>b</a>", 1, "x", "<a bool src='x'>b</a>")]
        public void HtmlWriter_ReplaceAttributeValue(string html, int index, string replacement, string expected)
        {
            var i = 0;
            var reader = new HtmlReader(html);
            var writer = new HtmlWriter(html.Length);

            while (reader.Read())
            {
                foreach (ref readonly var attribute in reader.Attributes)
                {
                    if (i++ == index)
                    {
                        writer.ReplaceAttributeValue(attribute, replacement);
                    }
                }
                writer.Write(reader);
            }

            Assert.Equal(expected, writer.ToString());
        }

        [Theory]
        [InlineData("<a></a>", "x", "<a x ></a>")]
        [InlineData("<a bool></a>", "x", "<a x  bool></a>")]
        public void HtmlWriter_InsertAttribute(string html, string attribute, string expected)
        {
            var reader = new HtmlReader(html);
            var writer = new HtmlWriter(html.Length);

            while (reader.Read())
            {
                writer.InsertAttribute(attribute);
                writer.Write(reader);
            }

            Assert.Equal(expected, writer.ToString());
        }
    }
}
