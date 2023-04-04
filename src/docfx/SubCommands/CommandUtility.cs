// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;

using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.SubCommands;

internal static class CommandUtility
{
    public static T GetConfig<T>(string configFile)
    {
        if (string.IsNullOrEmpty(configFile))
        {
            configFile = DocAsCode.Constants.ConfigFileName;
        }
        if (!File.Exists(configFile)) throw new FileNotFoundException($"Config file {configFile} does not exist!");

        return JsonUtility.Deserialize<T>(configFile);
    }
}
