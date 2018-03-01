// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    using System.Threading.Tasks;

    internal interface IHttpHandler
    {
        bool CanHandle(ServiceContext context);

        Task HandleAsync(ServiceContext context);
    }
}