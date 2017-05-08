// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;
    using Microsoft.DocAsCode;
    using Microsoft.DocAsCode.Plugins;

    [CommandOption("pdf", "Generate pdf file")]
    internal sealed class PdfCommandCreator : CommandCreator<PdfCommandOptions, PdfCommand>
    {
        public override PdfCommand CreateCommand(PdfCommandOptions options, ISubCommandController controller)
        {
            return new PdfCommand(options);
        }
    }
}