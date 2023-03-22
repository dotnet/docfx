// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.SubCommands;

[CommandOption("pdf", "Generate pdf file")]
internal sealed class PdfCommandCreator : CommandCreator<PdfCommandOptions, PdfCommand>
{
    public override PdfCommand CreateCommand(PdfCommandOptions options, ISubCommandController controller)
    {
        return new PdfCommand(options);
    }
}