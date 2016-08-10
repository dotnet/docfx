// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.VscPreview
{
    using System;
    using System.IO;
    using System.Text;

    using Microsoft.DocAsCode.Dfm;
    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Plugins;

    internal class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                try
                {
                    string command = Console.ReadLine();
                    if (command == "exit")
                        return;
                    else if (command == "dfmMarkup")
                    {
                        string basedir = Console.ReadLine();
                        // filename -> the relative path of the current file
                        string filename = Console.ReadLine();

                        // a simple protocol(get String According to the numOfRow and connect them)
                        string numStr = Console.ReadLine();
                        int numOfRow = Convert.ToInt32(numStr);
                        StringBuilder markdownContent = new StringBuilder();
                        for (int i = 0; i < numOfRow; i++)
                        {
                            markdownContent.AppendLine(Console.ReadLine());
                        }
                        var result = DfmMarkup(basedir, filename, markdownContent.ToString());

                        // append with customized endCode
                        Console.Write(result);
                        Console.Write('\a');
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"error:{e.Message}");
                }
            }
        }

        public static string DfmMarkup(string basedir, string filename, string markdownContent)
        {
            DfmServiceProvider dfmServiceProvider = new DfmServiceProvider();
            IMarkdownService dfmService = dfmServiceProvider.CreateMarkdownService(new MarkdownServiceParameters { BasePath = basedir});
            return dfmService.Markup(markdownContent, filename).Html;
        }
    }
}
