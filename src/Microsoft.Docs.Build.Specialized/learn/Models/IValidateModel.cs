// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.LearnValidation;

public interface IValidateModel
{
    string Uid { get; }

    string SourceRelativePath { get; set; }

    bool IsValid { get; set; }

    IValidateModel? Parent { get; set; }

    bool IsDeleted { get; set; }

    string? MSDate { get; set; }

    string ServiceData { get; set; }

    string PublishUpdatedAt { get; set; }

    string PageKind { get; set; }

    string? AssetId { get; set; }
}
