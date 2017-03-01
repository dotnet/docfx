// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    public class OutputFileCollection : ObservableDictionary<string, OutputFileInfo>
    {
        public bool ContainsValue(OutputFileInfo value) => Values.Contains(value);
    }
}
