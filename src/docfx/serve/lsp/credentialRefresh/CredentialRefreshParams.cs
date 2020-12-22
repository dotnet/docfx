// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using MediatR;
using OmniSharp.Extensions.JsonRpc;

namespace Microsoft.Docs.Build
{
    [Parallel]
    [Method("docfx/userCredentialRefresh", Direction.ServerToClient)]
    public record CredentialRefreshParams : IRequest<CredentialRefreshResponse>
    {
        public CredentialType Type { get; init; }
    }

    public record CredentialRefreshResponse
    {
        public CredentialRefreshResult? Result { get; init; }

        public ResponseError? Error { get; init; }
    }

    public record CredentialRefreshResult
    {
        public string? Token { get; init; }
    }
}
