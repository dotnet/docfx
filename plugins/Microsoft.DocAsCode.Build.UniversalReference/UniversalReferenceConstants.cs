// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.UniversalReference
{
    using Microsoft.DocAsCode.Common;

    internal static class UniversalReferenceConstants
    {
        public const string UniversalReference = "UniversalReference";
        public const string PythonReference = "PythonReference";
        public const string UniversalReferenceYamlMime = YamlMime.YamlMimePrefix + UniversalReference;
        public const string PythonReferenceYamlMime = YamlMime.YamlMimePrefix + PythonReference;
    }
}
