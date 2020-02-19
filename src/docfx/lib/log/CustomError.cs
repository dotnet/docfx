// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

#nullable enable

namespace Microsoft.Docs.Build
{
    [JsonConverter(typeof(ShortHandConverter))]
    internal class CustomError
    {
        public ErrorLevel? Severity { get; set; }

        public string? Code { get; set; }

        public string? AdditionalMessage { get; set; }

        public CustomError() { }

        public CustomError(ErrorLevel? severity) => Severity = severity;
    }
}
