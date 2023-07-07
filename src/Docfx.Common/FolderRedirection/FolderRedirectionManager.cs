// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Common;

public class FolderRedirectionManager
{
    private List<Rule> _rules = new();

    public FolderRedirectionManager(IEnumerable<FolderRedirectionRule> rules)
    {
        if (rules == null)
        {
            throw new ArgumentException(nameof(rules));
        }

        foreach (var rule in rules)
        {
            AddFolderRedirectionRule(rule.From, rule.To);
        }
    }

    private void AddFolderRedirectionRule(string from, string to)
    {
        if (from == null)
        {
            throw new ArgumentNullException(nameof(from));
        }
        if (to == null)
        {
            throw new ArgumentNullException(nameof(to));
        }
        var fromRel = (RelativePath)(from.TrimEnd('/', '\\') + "/");
        if (fromRel == null)
        {
            Logger.LogWarning($"invalid relative path for folder redirection rule source: {from}");
        }
        var toRel = (RelativePath)(to.TrimEnd('/', '\\') + "/");
        if (toRel == null)
        {
            Logger.LogWarning($"invalid relative path for folder redirection rule destination: {to}");
        }
        if (fromRel == toRel)
        {
            Logger.LogWarning($"ignore folder redirection rule as source({from}) and dest({to}) are the same.");
            return;
        }

        foreach (var redirection in _rules)
        {
            if (redirection.From.InDirectory(fromRel) || fromRel.InDirectory(redirection.From))
            {
                throw new ArgumentException($"Can't add redirection rule ({from})=>({to}): conflicts with the existing rule ({redirection.From})=>({redirection.To})", nameof(from));
            }
        }
        _rules.Add(new Rule(fromRel, toRel));
    }

    public RelativePath GetRedirectedPath(RelativePath file)
    {
        foreach (var redirection in _rules)
        {
            if (file.InDirectory(redirection.From))
            {
                return redirection.To + (file - redirection.From);
            }
        }
        return file;
    }

    private class Rule
    {
        public Rule(RelativePath from, RelativePath to)
        {
            From = from;
            To = to;
        }

        public RelativePath From { get; set; }

        public RelativePath To { get; set; }
    }
}
