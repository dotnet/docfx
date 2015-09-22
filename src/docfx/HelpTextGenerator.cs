// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using CommandLine;
    using CommandLine.Text;
    using System;
    using System.Linq;
    using System.Reflection;

    static class HelpTextGenerator
    {
        private static readonly HelpText HelpText = new HelpText
        {
            AdditionalNewLineAfterOption = false,
            AddDashesToOption = true
        };

        static HelpTextGenerator()
        {
            var assembly = Assembly.GetAssembly(typeof(HelpTextGenerator));
            var version = assembly.GetName()?.Version?.ToString();
            if (version != null) HelpText.Heading = new HeadingInfo("xdoc.exe", version);

            var copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;
            if (copyright != null) HelpText.Copyright = new CopyrightInfo(copyright, DateTime.Now.Year);
            var license = assembly.GetCustomAttribute<AssemblyLicenseAttribute>()?.Value;
            var usage = assembly.GetCustomAttribute<AssemblyUsageAttribute>()?.Value;

            AddLinesToHelpText(HelpText, license);
            AddLinesToHelpText(HelpText, usage);
        }

        public static string GetHelpMessage(Options options, string verb = null)
        {
            if (!string.IsNullOrEmpty(verb))
            {
                var subOption = GetSubOption(options, verb);
                if (subOption == null)
                {
                    HelpText.AddPreOptionsLine("Unknown command: " + verb);
                }
                else
                {
                    HelpText.AddPreOptionsLine(Environment.NewLine);
                    HelpText.AddPreOptionsLine("Usage: xdoc " + verb);
                    HelpText.AddOptions(subOption);
                }
            }

            return HelpText;
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

        private static object GetSubOption(Options options, string verb)
        {
            if (options == null || string.IsNullOrEmpty(verb)) return null;
            var property = typeof(Options).GetProperties().Where(s => s.GetCustomAttribute<VerbOptionAttribute>()?.LongName.ToLowerInvariant() == verb).FirstOrDefault();
            if (property == null) return null;

            return property.GetValue(options);
        }
    }
}
