// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.VscPreview
{
    using System;

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
                    string result;
                    switch (command.ToLower().Trim())
                    {
                        case "exit":
                            return;
                        case "docfxpreview":
                            result = DocfxProcess.DocFxProcess();
                            SendWithEndCode(result);
                            break;
                        case "tokentreepreview":
                            result = TokenTreeProcess.TokenTreePreview(dfmMarkdownService);
                            SendWithEndCode(result);
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

        private static void SendWithEndCode(string result)
        {
            // Append with customized endCode
            Console.Write(result);
            Console.Write('\a');
        }
    }
}
