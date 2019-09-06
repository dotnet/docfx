// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal interface IMicrosoftGraphClient
    {
        /// <summary>
        /// Returns null if the specified alias is a valid microsoft alias,
        /// or the corresponding error.
        /// </summary>
        Task<Error> IsValidMicrosoftAlias(string alias);
    }
}
