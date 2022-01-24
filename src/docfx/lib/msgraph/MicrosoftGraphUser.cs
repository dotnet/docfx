// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal class MicrosoftGraphUser : ICacheObject<string>
{
    public string? Alias { get; set; }

    public string? Id { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public IEnumerable<string> GetKeys()
    {
        if (Alias != null)
        {
            yield return Alias;
        }
    }
}
