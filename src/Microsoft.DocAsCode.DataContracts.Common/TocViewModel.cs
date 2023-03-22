// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.DataContracts.Common;

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
