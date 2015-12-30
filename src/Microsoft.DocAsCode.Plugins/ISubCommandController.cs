// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    public interface ISubCommandController
    {
        bool TryGetCommandCreator(string name, out ISubCommandCreator creator);
        string GetHelpText();
    }
}
