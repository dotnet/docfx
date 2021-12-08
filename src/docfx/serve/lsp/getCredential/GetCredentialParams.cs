// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MediatR;
using OmniSharp.Extensions.JsonRpc;

namespace Microsoft.Docs.Build;

[Serial]
[Method("docfx/getCredential", Direction.ServerToClient)]
internal record GetCredentialParams : IRequest<GetCredentialResponse>
{
    public string Url { get; init; } = string.Empty;
}

internal record GetCredentialResponse
{
    public Dictionary<string, HttpConfig> Http { get; init; } = new();
}
