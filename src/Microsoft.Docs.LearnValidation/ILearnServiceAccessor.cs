// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Docs.LearnValidation
{
    public interface ILearnServiceAccessor
    {
        Task<string> HierarchyDrySync(string body);

        Task<HttpResponseMessage> CheckLearnPathItemExist(string branch, string locale, string uid, bool isModule);
    }
}
