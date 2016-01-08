// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;
    using Microsoft.DocAsCode;
    using Microsoft.DocAsCode.Plugins;

    [CommandOption("template", "List or export existing template")]
    internal sealed class TemplateCommandCreator : CommandCreator<TemplateCommandOptions, TemplateCommand>
    {
        public override TemplateCommand CreateCommand(TemplateCommandOptions options, ISubCommandController controller)
        {
            return new TemplateCommand(options);
        }
    }
}