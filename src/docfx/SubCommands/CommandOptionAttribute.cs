// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Composition;

using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.SubCommands;

[MetadataAttribute]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class CommandOptionAttribute : ExportAttribute
{
    public string Name { get; }
    public string HelpText { get; }
    public CommandOptionAttribute(string name, string helpText) : base(typeof(ISubCommandCreator))
    {
        Name = name;
        HelpText = helpText;
    }
}
