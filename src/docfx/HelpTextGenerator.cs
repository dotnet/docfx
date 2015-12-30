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
            if (version != null) HelpText.Heading = new HeadingInfo("docfx", version);

            var copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;
            if (copyright != null) HelpText.Copyright = new CopyrightInfo(copyright, DateTime.Now.Year);
            var license = assembly.GetCustomAttribute<AssemblyLicenseAttribute>()?.Value;
            var usage = assembly.GetCustomAttribute<AssemblyUsageAttribute>()?.Value;

            AddLinesToHelpText(HelpText, license);
            AddLinesToHelpText(HelpText, usage);
        }

        public static string GetHelpMessage(Options options, string verb = null)
        {
            var helpText = new HelpText(HelpText);
            if (!string.IsNullOrEmpty(verb))
            {
                var subOption = GetSubOption(options, verb);
                if (subOption == null)
                {
                    helpText.AddPreOptionsLine("Unknown command: " + verb);
                }
                else
                {
                    helpText.AddPreOptionsLine(Environment.NewLine);
                    helpText.AddPreOptionsLine("Usage: docfx " + verb);
                    helpText.AddOptions(subOption);
                }
            }

            return helpText;
        }

        public static string GetHeader()
        {
            return new HelpText(HelpText);
        }

        public static string GetHelpMessage(object options)
        {
            var helpText = new HelpText(HelpText)
            {
                AdditionalNewLineAfterOption = false,
                AddDashesToOption = true
            };
            helpText.AddOptions(options);
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

        private static object GetSubOption(Options options, string verb)
        {
            if (options == null || string.IsNullOrEmpty(verb)) return null;
            var property = typeof(Options).GetProperties().Where(s => s.GetCustomAttribute<VerbOptionAttribute>()?.LongName.ToLowerInvariant() == verb).FirstOrDefault();
            if (property == null) return null;

            return property.GetValue(options);
        }
    }
}
