// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine.Text;
using System.Reflection;

namespace Microsoft.DocAsCode;

static class HelpTextGenerator
{
    private static readonly HelpText HelpText = new()
    {
        AdditionalNewLineAfterOption = false,
        AddDashesToOption = true
    };

    private static readonly string GeneralUsage;
    private static readonly string ProductName;

    static HelpTextGenerator()
    {
        var assembly = Assembly.GetAssembly(typeof(HelpTextGenerator));
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var name = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "docfx";
        if (version != null) HelpText.Heading = new HeadingInfo(name, version);
        var usage = string.Join('\n',
            "",
            "",
            "   Usage1: docfx <docfx.json file path> [-o <output folder path>]",
            "   Usage2: docfx <subcommand> [<args>]",
            "",
            "See 'docfx help <command> to read about a specific subcommand guide");
        ProductName = name;
        GeneralUsage = usage;
    }

    public static string GetHeader()
    {
        var helpText = GetVersion();
        AddLinesToHelpText(helpText, GeneralUsage);
        return helpText;
    }

    public static HelpText GetVersion()
    {
        return new HelpText(HelpText)
        {
            AdditionalNewLineAfterOption = false,
            AddDashesToOption = true
        };
    }

    public static string GetSubCommandHelpMessage(object option, string[] usages)
    {
        var helpText = new HelpText(HelpText)
        {
            AdditionalNewLineAfterOption = false,
            AddDashesToOption = true
        };

        bool multiple = usages.Length > 1;
        for (int i = 0; i < usages.Length; i++)
        {
            string title = multiple ? $"Usage{i + 1}" : "Usage";
            AddLinesToHelpText(helpText, $"{Environment.NewLine}{title}: {ProductName} {usages[i]}");
        }

        helpText.AddOptions(option);
        return helpText.ToString();
    }

    private static void AddLinesToHelpText(HelpText helpText, string message)
    {
        if (message == null) return;
        else
        {
            foreach (var i in message.Split('\n'))
            {
                helpText.AddPreOptionsLine(i);
            }
        }
    }
}
