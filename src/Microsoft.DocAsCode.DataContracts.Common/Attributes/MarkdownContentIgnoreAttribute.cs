// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.DataContracts.Common
{
    using System;

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class MarkdownContentIgnoreAttribute : Attribute
    {
    }
}
