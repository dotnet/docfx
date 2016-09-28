// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.VscPreview
{
    using System;
    using System.Text;

    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Plugins;

    internal class DfmProcess
    {
        static void Main(string[] args)
        {
            DfmJsonTokenTreeServiceProvider dfmJsonTokenTreeServiceProvider = new DfmJsonTokenTreeServiceProvider();
            IMarkdownService dfmMarkdownService = dfmJsonTokenTreeServiceProvider.CreateMarkdownService(new MarkdownServiceParameters());
            while (true)
            {
                try
                {
                    string command = Console.ReadLine();
                    switch (command.ToLower().Trim())
                    {
                        case "exit":
                            return;
                        case "dfmmarkup":
                            DfmMarkupReceiveContent();
                            break;
                        case "jsonmarkup":
                            JsonMarkupReceiveContent(dfmMarkdownService);
                            break;
                        default:
                            SendWithEndCode("Undefined Command");
                            continue;
                    }
                }
                catch (Exception e)
                {
                    SendWithEndCode($"error:{e.Message}");
                }
            }
        }

        private static void DfmMarkupReceiveContent()
        {
            string basedir = Console.ReadLine();
            // filename is the relative path of the current file
            string filename = Console.ReadLine();
            string markdownContent = GetMarkdownContent();
            var result = DfmMarkup(basedir, filename, markdownContent.ToString());

            SendWithEndCode(result);
        }

        private static void JsonMarkupReceiveContent(IMarkdownService dfmMarkdownService)
        {
            string markdownContent = GetMarkdownContent();
            var result = JsonMarkup(dfmMarkdownService, markdownContent.ToString());

            SendWithEndCode(result);
        }

        private static string DfmMarkup(string basedir, string filename, string markdownContent)
        {
            // TODO: different editor use different child process so there is no need to create dfm service each time
            DfmServiceProvider dfmServiceProvider = new DfmServiceProvider();
            IMarkdownService dfmService = dfmServiceProvider.CreateMarkdownService(new MarkdownServiceParameters {BasePath = basedir});
            return dfmService.Markup(markdownContent, filename).Html;
        }

        private static string JsonMarkup(IMarkdownService dfmMarkdownService, string markdownContent)
        {
            return dfmMarkdownService.Markup(markdownContent, null).Html;
        }

        private static string GetMarkdownContent()
        {
            // a simple protocol(get String According to the numOfRow and connect them)
            string numStr = Console.ReadLine();
            int numOfRow = Convert.ToInt32(numStr);
            StringBuilder markdownContent = new StringBuilder();
            for (int i = 0; i < numOfRow; i++)
            {
                markdownContent.AppendLine(Console.ReadLine());
            }
            return markdownContent.ToString();
        }

        private static void SendWithEndCode(string result)
        {
            // append with customized endCode
            Console.Write(result);
            Console.Write('\a');
        }
    }
}
