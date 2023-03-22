// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.SubCommands;

[CommandOption("template", "List or export existing template")]
internal sealed class TemplateCommandCreator : CommandCreator<TemplateCommandOptions, TemplateCommand>
{
    public override TemplateCommand CreateCommand(TemplateCommandOptions options, ISubCommandController controller)
    {
        return new TemplateCommand(options);
    }
}