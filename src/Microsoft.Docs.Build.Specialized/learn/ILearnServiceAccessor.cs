// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.LearnValidation;

public interface ILearnServiceAccessor
{
    Task<string> HierarchyDrySync(string body);

    Task<bool> CheckLearnPathItemExist(string branch, string locale, string uid, CheckItemType type);
}
