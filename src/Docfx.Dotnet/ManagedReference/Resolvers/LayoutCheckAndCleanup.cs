// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;

using Docfx.Common;

namespace Docfx.Dotnet;

internal class LayoutCheckAndCleanup : IResolverPipeline
{
    /// <summary>
    /// The yaml layout should be 
    /// namespace -- class level -- method level
    /// But also allows nested namespaces
    /// </summary>
    /// <param name="allMembers"></param>
    /// <returns></returns>
    public void Run(MetadataModel yaml, ResolverContext context)
    {
        StringBuilder message = new();
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

    private static string CheckNamespaces(MetadataItem member)
    {
        StringBuilder message = new();

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
                Logger.Log(LogLevel.Error, $"Invalid item inside yaml metadata: {i.Type} is not allowed inside {member.Type}. Will be ignored.");
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
    private static string CheckNamespaceMembers(MetadataItem member)
    {
        StringBuilder message = new();

        // Skip if it is already invalid
        if (member.Items == null || member.IsInvalid)
        {
            return string.Empty;
        }

        foreach (var i in member.Items)
        {
            var result = CheckNamespaceMembersMembers(i);
            if (!string.IsNullOrEmpty(result))
            {
                message.AppendLine(result);
            }
        }

        return message.ToString();
    }

    /// <summary>
    /// e.g. Methods
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    private static string CheckNamespaceMembersMembers(MetadataItem member)
    {
        StringBuilder message = new();
        if (member.IsInvalid)
        {
            return string.Empty;
        }

        return message.ToString();
    }
}
