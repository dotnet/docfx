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
            try
            {
                while (true)
                {
                    // get the path;
                    string path = Console.ReadLine();                   // path -> basedir
                    if (path == "exit")
                        break;
                    string filename = Console.ReadLine();               // filename -> the relative path of the current file 

                    string numStr = Console.ReadLine();
                    int numOfRow = Convert.ToInt32(numStr);          // a simple protocal
                    StringBuilder markdownContent = new StringBuilder();
                    for (int i = 0; i < numOfRow; i++)
                    {
                        markdownContent.AppendLine(Console.ReadLine());
                    }
                    DfmServiceProvider dfmServiceProvider = new DfmServiceProvider();
                    MarkdownServiceParameters markdownServiceParameters = new MarkdownServiceParameters();
                    markdownServiceParameters.BasePath = path;
                    IMarkdownService iMarkdownService = dfmServiceProvider.CreateMarkdownService(markdownServiceParameters);
                    var result = iMarkdownService.Markup(markdownContent.ToString(), filename).Html;

                    // append with customized endCode
                    Console.Write(result);
                    Console.Write('\a');
                }
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine($"error:{e.Message}");
                return;
            }
        }
    }
}
