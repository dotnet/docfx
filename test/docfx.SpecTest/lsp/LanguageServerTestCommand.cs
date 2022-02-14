// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

public class LanguageServerTestCommand
{
    public Dictionary<string, string> OpenFiles { get; init; }

    public Dictionary<string, string> EditFiles { get; init; }

    public Dictionary<string, string> CreateFiles { get; init; }

    public Dictionary<string, string> EditFilesWithoutEditor { get; init; }

    public List<string> CloseFiles { get; init; }

    public List<string> DeleteFiles { get; init; }

    public Dictionary<string, JToken> ExpectDiagnostics { get; init; }

    public JToken ExpectGetCredentialRequest { get; init; }

    public JToken Response { get; init; }

    public bool ExpectNoNotification { get; init; }
}
