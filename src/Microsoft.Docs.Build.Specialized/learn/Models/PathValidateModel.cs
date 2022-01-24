// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.TripleCrown.Hierarchy.DataContract.Hierarchy;
using Newtonsoft.Json;

namespace Microsoft.Docs.LearnValidation;

public class PathValidateModel : PathSyncModel, IValidateModel
{
    [JsonProperty("source_relative_path")]
    public string SourceRelativePath { get; set; } = "";

    public bool IsValid { get; set; }

    public bool IsDeleted { get; set; }

    public string Uid => UId;

    public IValidateModel? Parent { get; set; }

    public string? MSDate { get; set; }

    public string ServiceData { get; set; } = "";

    public string PublishUpdatedAt { get; set; } = "";

    public string PageKind { get; set; } = "";
}
