// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Globalization;

namespace Docfx;

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
