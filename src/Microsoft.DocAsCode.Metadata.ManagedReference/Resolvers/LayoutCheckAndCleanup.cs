// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.Diagnostics;
    using System.Text;

    using Microsoft.DocAsCode.Common;

    public class LayoutCheckAndCleanup : IResolverPipeline
    {
        /// <summary>
        /// The yaml layout should be 
        /// namespace -- class level -- method level
        /// </summary>
        /// <param name="allMembers"></param>
        /// <returns></returns>
        public void Run(MetadataModel yaml, ResolverContext context)
        {
            StringBuilder message = new StringBuilder();
            foreach (var member in yaml.TocYamlViewModel.Items)
            {
                var result = CheckNamespaces(member);
                if (!string.IsNullOrEmpty(result))
                {
                    message.AppendLine(result);
                }
            }

            if (message.Length > 0)
            {
                Logger.LogWarning(message.ToString());
            }
        }

        private string CheckNamespaces(MetadataItem member)
        {
            StringBuilder message = new StringBuilder();

            // Skip if it is already invalid
            if (member.Items == null || member.IsInvalid)
            {
                return string.Empty;
            }

            foreach (var i in member.Items)
            {
                Debug.Assert(i.Type.IsPageLevel());
                if (!i.Type.IsPageLevel())
                {
                    Logger.Log(LogLevel.Error, $"Invalid item inside yaml metadata: {i.Type.ToString()} is not allowed inside {member.Type.ToString()}. Will be ignored.");
                    message.AppendFormat("{0} is not allowed inside {1}.", i.Type.ToString(), member.Type.ToString());
                    i.IsInvalid = true;
                }
                else
                {
                    var result = CheckNamespaceMembers(i);
                    if (!string.IsNullOrEmpty(result))
                    {
                        message.AppendLine(result);
                    }
                }
            }

            return message.ToString();
        }

        /// <summary>
        /// e.g. Classes
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private string CheckNamespaceMembers(MetadataItem member)
        {
            StringBuilder message = new StringBuilder();

            // Skip if it is already invalid
            if (member.Items == null || member.IsInvalid)
            {
                return string.Empty;
            }

            foreach (var i in member.Items)
            {
                Debug.Assert(!i.Type.IsPageLevel());
                if (i.Type.IsPageLevel())
                {
                    Logger.Log(LogLevel.Error, $"Invalid item inside yaml metadata: {i.Type.ToString()} is not allowed inside {member.Type.ToString()}. Will be ignored.");
                    message.AppendFormat("{0} is not allowed inside {1}.", i.Type.ToString(), member.Type.ToString());
                    i.IsInvalid = true;
                }
                else
                {
                    var result = CheckNamespaceMembersMembers(i);
                    if (!string.IsNullOrEmpty(result))
                    {
                        message.AppendLine(result);
                    }
                }
            }

            return message.ToString();
        }


        /// <summary>
        /// e.g. Methods
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private string CheckNamespaceMembersMembers(MetadataItem member)
        {
            StringBuilder message = new StringBuilder();
            if (member.IsInvalid)
            {
                return string.Empty;
            }

            // does method has members?
            Debug.Assert(member.Items == null);
            if (member.Items != null)
            {
                foreach (var i in member.Items)
                {
                    i.IsInvalid = true;
                }

                Logger.Log(LogLevel.Error, $"Invalid item inside yaml metadata: {member.Type.ToString()} should not contain items. Will be ignored.");
                message.AppendFormat("{0} should not contain items.", member.Type.ToString());
            }

            return message.ToString();
        }
    }
}
