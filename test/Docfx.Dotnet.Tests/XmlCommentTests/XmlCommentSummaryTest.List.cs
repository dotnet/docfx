// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Docfx.Dotnet.Tests;

public partial class XmlCommentSummaryTest
{
    [Fact]
    public void ListTypeTable()
    {
        ValidateSummary(
            // Input XML
            """
            <summary>
            Paragraph1
            <list type="table">
              <item>
                <term>Summary</term>
                <description>Description</description>
              </item>
            </list>
            Paragraph2
            </summary>
            """,
            // Expected Markdown
            """"
            Paragraph1

            <table><tbody><tr><td class="term">Summary</td><td class="description">Description</td></tr></tbody></table>

            Paragraph2
            """");
    }

    [Fact]
    public void ListTypeBullet()
    {
        ValidateSummary(
            // Input XML
            """
            <summary>
            Paragraph1
            <list type="bullet">
              <item>
                <term>Summary</term>
                <description>Description</description>
              </item>
            </list>
            Paragraph2
            </summary>
            """,
            // Expected Markdown
            """
            Paragraph1

            <ul><li><span class="term">Summary</span>Description</li></ul>

            Paragraph2
            """);
    }

    [Fact]
    public void ListTypeNumber()
    {
        ValidateSummary(
            // Input XML
            """
            <summary>
            Paragraph1
            <list type="number">
              <item>
                <term>Summary</term>
                <description>Description</description>
              </item>
            </list>
            Paragraph2
            </summary>
            """,
            // Expected Markdown
            """
            Paragraph1

            <ol><li><span class="term">Summary</span>Description</li></ol>

            Paragraph2
            """);
    }

    [Fact]
    public void ListWithCode()
    {
        ValidateSummary(
            // Input XML
            """
            <summary>
            Paragraph1
            <list type='bullet'>
                <item>
                    <description>
                        <code language = 'c#'>
                        public class XmlElement
                            : XmlLinkedNode
                        </code>
                    </description>
                </item>
            </list>
            Paragraph2
            </summary>
            """,
            // Expected Markdown
            """
            Paragraph1

            <ul><li>

                        <pre><code class="lang-c#">public class XmlElement
                : XmlLinkedNode</code></pre>

                    </li></ul>

            Paragraph2
            """);
    }

    [Fact]
    public void ListComplex()
    {
        ValidateSummary(
            // Input XML
            """
            <summary>
              <see href="https://example.org"/>
              <see href="https://example.org">example</see>
              <para>This is <paramref name='ref'/><paramref /> a sample of exception node</para>
              <list type='bullet'>
                <item>
                  <description>
                  <code language = 'c#'>
                  public class XmlElement
                    : XmlLinkedNode
                  </code>
                  <list type='number'>
                    <item>
                      <description>
                      word inside list->listItem->list->listItem->para.>
                      the second line.
                      </description>
                    </item>
                    <item>
                      <description>item2 in numbered list</description>
                    </item>
                  </list>
                  </description>
                </item>
                <item>
                  <description>item2 in bullet list</description>
                </item>
                <item>
                  loose text <i>not</i> wrapped in description
                </item>
              </list>
            </summary>
            }
            """,
            // Expected Markdown
            """
            <a href="https://example.org">https://example.org</a>
            <a href="https://example.org">example</a>
            <p>This is <code class="paramref">ref</code> a sample of exception node</p>
            <ul><li>

                <pre><code class="lang-c#">public class XmlElement
              : XmlLinkedNode</code></pre>

                <ol><li>
                    word inside list->listItem->list->listItem->para.>
                    the second line.
                    </li><li>item2 in numbered list</li></ol>
                </li><li>item2 in bullet list</li><li>
                loose text <i>not</i> wrapped in description
              </li></ul>
            """);
    }
}




