// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using Microsoft.DocAsCode.EntityModel;

    interface ISubCommand
    {
        ParseResult Exec(Options options);
    }

    enum SubCommandType
    {
        Init,
        Help,
        Metadata,
        Build,
        Website,
        Export,
        Pack,
        Serve,
    }
}
