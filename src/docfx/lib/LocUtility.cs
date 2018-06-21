// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Globalization;

namespace Microsoft.Docs.Build
{
    internal static class LocUtility
    {
        public static CultureInfo GetCultureInfo(string locale)
        {
            Debug.Assert(!string.IsNullOrEmpty(locale));

            try
            {
                return new CultureInfo(locale);
            }
            catch (CultureNotFoundException)
            {
                return new CultureInfo("en-US");
            }
        }
    }
}
