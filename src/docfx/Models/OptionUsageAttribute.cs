// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class OptionUsageAttribute : Attribute
    {
        public string Name { get; }

        public OptionUsageAttribute(string name)
        {
            Name = name;
        }
    }
}