// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Globalization;

namespace Microsoft.DocAsCode;

internal class ArrayOptionConverter : TypeConverter
{
    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        return value is string && (destinationType == typeof(IEnumerable<string>) || destinationType == typeof(string[]));
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is string str)
        {
            return str.Split(',', StringSplitOptions.RemoveEmptyEntries);
        }

        return base.ConvertFrom(context, culture, value);
    }
}
