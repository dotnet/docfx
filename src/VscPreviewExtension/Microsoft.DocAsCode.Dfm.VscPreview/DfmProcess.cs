// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.VscPreview
{
    using System;
    using System.Text;

    internal class DfmProcess
    {
        static void Main()
        {
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
                            result = DocfxProcessor.DocfxProcess(GetMarkdownContent());
                            SendWithEndCode(result);
                            break;
                        case "tokentreepreview":
                            result = TokenTreeProcessor.TokenTreePreview(GetMarkdownContent());
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

        private static string GetMarkdownContent()
        {
            // A simple protocol(get String According to the numOfRow and connect them)
            string numStr = Console.ReadLine();
            int numOfRow = Convert.ToInt32(numStr);
            StringBuilder markdownContent = new StringBuilder();
            for (int i = 0; i < numOfRow; i++)
            {
                markdownContent.AppendLine(Console.ReadLine());
            }
            return markdownContent.ToString();
        }
    }
}
