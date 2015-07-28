// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System;

    class SubCommandFactory
    {
        public static ISubCommand GetCommand(SubCommandType type)
        {
            switch (type)
            {
                case SubCommandType.Init:
                    return new InitSubCommand();
                case SubCommandType.Help:
                    return new HelpSubCommand();
                case SubCommandType.Metadata:
                    return new MetadataSubCommand();
                case SubCommandType.Website:
                    return new WebsiteSubCommand();
                case SubCommandType.Export:
                    return new ExportSubCommand();
                case SubCommandType.Pack:
                    return new PackSubCommand();
                default:
                    throw new NotSupportedException("SubCommandType: " + type.ToString(), null);
            }
        }
    }
}
