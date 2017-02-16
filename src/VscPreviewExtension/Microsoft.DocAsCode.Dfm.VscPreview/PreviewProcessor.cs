// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.VscPreview
{
    using System;
    using System.Text;

    public class PreviewProcessor
    {
        protected static string GetMarkdownContent()
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
