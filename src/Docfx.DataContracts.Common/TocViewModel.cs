// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.DataContracts.Common;

[Serializable]
public class TocViewModel
    : List<TocItemViewModel>
{
    public TocViewModel(IEnumerable<TocItemViewModel> items) : base(items) { }
    public TocViewModel() : base() { }

    public TocViewModel Clone()
    {
        return new TocViewModel(ConvertAll(s => s.Clone()));
    }
}
