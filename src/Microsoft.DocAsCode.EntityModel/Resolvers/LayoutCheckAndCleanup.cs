// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System.Diagnostics;
    using System.Text;

    public class LayoutCheckAndCleanup : IResolverPipeline
    {
        /// <summary>
        /// The yaml layout should be 
        /// namespace -- class level -- method level
        /// </summary>
        /// <param name="allMembers"></param>
        /// <returns></returns>
        public ParseResult Run(MetadataModel yaml, ResolverContext context)
        {
            ParseResult overall = new ParseResult(ResultLevel.Success);
            StringBuilder message = new StringBuilder();
            foreach (var member in yaml.TocYamlViewModel.Items)
            {
                CheckNamespaces(member);
            }

            if (message.Length > 0)
            {
                overall.ResultLevel = ResultLevel.Warning;
                overall.Message = message.ToString();
            }

            return overall;
        }

        private ParseResult CheckNamespaces(MetadataItem member)
        {
            ParseResult overall = new ParseResult(ResultLevel.Success);
            StringBuilder message = new StringBuilder();

            // Skip if it is already invalid
            if (member.Items == null || member.IsInvalid)
            {
                return overall;
            }

            foreach (var i in member.Items)
            {
                Debug.Assert(i.Type.IsPageLevel());
                if (!i.Type.IsPageLevel())
                {
                    ParseResult.WriteToConsole(ResultLevel.Error, "Invalid item inside yaml metadata: {0} is not allowed inside {1}. Will be ignored.", i.Type.ToString(), member.Type.ToString());
                    message.AppendFormat("{0} is not allowed inside {1}.", i.Type.ToString(), member.Type.ToString());
                    i.IsInvalid = true;
                }
                else
                {
                    ParseResult result = CheckNamespaceMembers(i);
                    if (!string.IsNullOrEmpty(result.Message))
                    {
                        message.AppendLine(result.Message);
                    }
                }
            }

            if (message.Length > 0)
            {
                overall.ResultLevel = ResultLevel.Warning;
                overall.Message = message.ToString();
            }

            return overall;
        }

        /// <summary>
        /// e.g. Classes
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private ParseResult CheckNamespaceMembers(MetadataItem member)
        {
            ParseResult overall = new ParseResult(ResultLevel.Success);
            StringBuilder message = new StringBuilder();

            // Skip if it is already invalid
            if (member.Items == null || member.IsInvalid)
            {
                return overall;
            }


            foreach (var i in member.Items)
            {
                Debug.Assert(!i.Type.IsPageLevel());
                if (i.Type.IsPageLevel())
                {
                    ParseResult.WriteToConsole(ResultLevel.Error, "Invalid item inside yaml metadata: {0} is not allowed inside {1}. Will be ignored.", i.Type.ToString(), member.Type.ToString());
                    message.AppendFormat("{0} is not allowed inside {1}.", i.Type.ToString(), member.Type.ToString());
                    i.IsInvalid = true;
                }
                else
                {
                    ParseResult result = CheckNamespaceMembersMembers(i);
                    if (!string.IsNullOrEmpty(result.Message))
                    {
                        message.AppendLine(result.Message);
                    }
                }
            }

            if (message.Length > 0)
            {
                overall.ResultLevel = ResultLevel.Warning;
                overall.Message = message.ToString();
            }

            return overall;
        }


        /// <summary>
        /// e.g. Methods
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private ParseResult CheckNamespaceMembersMembers(MetadataItem member)
        {
            ParseResult overall = new ParseResult(ResultLevel.Success);
            StringBuilder message = new StringBuilder();
            if (member.IsInvalid)
            {
                return overall;
            }

            // does method has members?
            Debug.Assert(member.Items == null);
            if (member.Items != null)
            {
                foreach (var i in member.Items)
                {
                    i.IsInvalid = true;
                }

                ParseResult.WriteToConsole(ResultLevel.Error, "Invalid item inside yaml metadata: {0} should not contain items. Will be ignored.", member.Type.ToString());
                message.AppendFormat("{0} should not contain items.", member.Type.ToString());
            }

            if (message.Length > 0)
            {
                overall.ResultLevel = ResultLevel.Warning;
                overall.Message = message.ToString();
            }

            return overall;
        }
    }
}
