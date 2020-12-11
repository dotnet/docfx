// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    internal record FileActionEvent
    {
        public FileActionType Type { get; }

        public string FilePath { get; }

        public string? Content { get; }

        public FileActionEvent(FileActionType type, string filePath, string? content)
            => (Type, FilePath, Content) = (type, filePath, content);
    }
}
