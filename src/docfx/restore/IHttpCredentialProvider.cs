// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;

namespace Microsoft.Docs.Build
{
    internal interface IHttpCredentialProvider
    {
        void ProvideCredential(HttpRequestMessage request);
    }
}
