// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Docs.Build;

internal record AdditionalErrorInfo
{
    [JsonProperty("ms.author")]
    public string? MsAuthor { get; init; }

    [JsonProperty("ms.prod")]
    public string? MsProd { get; init; }

    [JsonProperty("ms.technology")]
    public string? MsTechnology { get; init; }

    [JsonProperty("ms.service")]
    public string? MsService { get; init; }

    [JsonProperty("ms.subservice")]
    public string? MsSubservice { get; init; }

    [JsonProperty("ms.topic")]
    public string? MsTopic { get; init; }

    public AdditionalErrorInfo(string? msAuthor, string? msProd, string? msTechnology, string? msService, string? msSubservice, string? msTopic)
    {
        MsAuthor = msAuthor;
        MsProd = msProd;
        MsTechnology = msTechnology;
        MsService = msService;
        MsSubservice = msSubservice;
        MsTopic = msTopic;
    }
}
