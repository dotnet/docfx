// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using Microsoft.DocAsCode.EntityModel;

    interface IHasLog
    {
        string Log { get; }
        LogLevel? LogLevel { get; }
    }
}
