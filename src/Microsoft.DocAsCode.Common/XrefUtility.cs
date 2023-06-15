// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.Common;

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
