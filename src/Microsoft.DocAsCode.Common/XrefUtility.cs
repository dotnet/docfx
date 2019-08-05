// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using Microsoft.DocAsCode.Plugins;

    public static class XrefUtility
    {
        public static bool TryGetXrefStringValue(this XRefSpec spec, string key, out string value)
        {

            if (spec.TryGetValue(key, out var objValue) && objValue != null)
            {
                if (objValue is string)
                {
                    value = (string)objValue;
                    return true;
                }
                else
                {
                    Logger.LogWarning(
                        $"The value of property '{key}' in uid '{spec.Uid}' is not string",
                        code: WarningCodes.Build.ReferencedXrefPropertyNotString);
                }
            }
            value = null;
            return false;
        }
    }
}
