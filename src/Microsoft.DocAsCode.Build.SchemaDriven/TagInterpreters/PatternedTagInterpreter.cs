// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven.Processors
{
    using System;
    using System.Composition;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    using Newtonsoft.Json.Linq;

    [Export(typeof(ITagInterpreter))]
    internal class PatternedTagInterpreter : ITagInterpreter
    {
        private const string Prefix = "patterned:";

        public int Order => 1;

        public bool Matches(string tagName)
        {
            return tagName.Length > Prefix.Length && tagName.StartsWith(Prefix, StringComparison.Ordinal);
        }

        public object Interpret(string tagName, BaseSchema schema, object value, IProcessContext context, string path)
        {
            Validate(tagName, value, context, path);
            return value;
        }

        private void Validate(string tagName, object value, IProcessContext context, string path)
        {
            var val = value as string;
            if (val == null)
            {
                Logger.LogWarning($"Tag {tagName} can not be tagged to {path} as type of the value is {value.GetType()}.", code: WarningCodes.Build.InvalidTaggedPropertyType);
                return;
            }

            if (TryGetTagParameter(tagName, context, out var pattern))
            {
                if (!new Regex(pattern).Match(val).Success)
                {
                    var errorMessage = $"Property {path} with value \"{val}\" is not in valid format, it must follow regular expression \"{pattern}\".";
                    Logger.LogError(errorMessage, code: ErrorCodes.Build.InvalidPropertyFormat);
                    throw new DocumentException(errorMessage);
                }
            }
        }

        private bool TryGetTagParameter(string tagName, IProcessContext context, out string pattern)
        {
            JArray args = null;
            if (context.Host?.BuildParameters?.TagParameters?.TryGetValue(tagName, out args) == true)
            {
                if (args?.Count != 1)
                {
                    Logger.LogWarning($"Invalid tagParameters config: tag {tagName} interpreter only supports one parameter, {tagName} interpreter will be ignored.", code: WarningCodes.Build.InvalidTagParametersConfig);
                }
                else
                {
                    pattern = args[0].Value<string>();
                    if (pattern == null)
                    {
                        Logger.LogWarning($"Invalid tagParameters config: tag {tagName} interpreter only supports parameter in string type, {tagName} interpreter will be ignored.", code: WarningCodes.Build.InvalidTagParametersConfig);
                        return false;
                    }
                    return true;
                }
            }

            // Less-strict check here without log warning, considering the time gap between schema update and docfx.json update
            pattern = null;
            return false;
        }
    }
}
