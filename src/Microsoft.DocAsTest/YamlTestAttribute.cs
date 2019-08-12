// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;

namespace Microsoft.DocAsTest
{
    /// <summary>
    /// Provides test data coming from YAML documents.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class YamlTestAttribute : Attribute, ITestAttribute
    {
        private static readonly char[] s_summaryTrimChars = { '#', ' ', '\t' };

        /// <summary>
        /// Gets or sets the glob pattern to search for files.
        /// </summary>
        public string Glob { get; set; }

        public YamlTestAttribute(string glob = null) => Glob = glob;

        void ITestAttribute.DiscoverTests(string path, Action<TestData> report)
        {
            using (var reader = File.OpenText(path))
            {
                var ordinal = 0;
                var lineNumber = 0;
                var data = new TestData { LineNumber = 1 };
                var content = new StringBuilder();

                while (true)
                {
                    var line = reader.ReadLine();

                    lineNumber++;

                    if (line is null || line == "---")
                    {
                        if (content.Length > 0)
                        {
                            data.Ordinal = ++ordinal;
                            data.Content = content.ToString();
                            data.FilePath = path;

                            report(data);

                            content.Length = 0;
                            data = new TestData { LineNumber = lineNumber + 1 };
                        }

                        if (line is null)
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (line.StartsWith("#") && data.Summary is null)
                        {
                            data.Summary = line.Trim(s_summaryTrimChars);
                        }

                        content.Append(line);
                        content.AppendLine();
                    }
                }
            }
        }
    }
}
