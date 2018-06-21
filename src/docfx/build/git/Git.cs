// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal static class Git
    {
        public static void Process(PageModel model, Document document, GitRepoInfoProvider repo)
        {
            Debug.Assert(document != null);
            Debug.Assert(repo != null);
            if (!repo.ProfileInitialized)
                return;

            // TODO: support specifed authorName and updatedAt
            GitUserProfile authorInfo = null;
            if (repo.TryGetCommits(document, out var commits))
            {
                for (var i = commits.Count - 1; i >= 0; i--)
                {
                    if (!string.IsNullOrEmpty(commits[i].AuthorEmail))
                    {
                        authorInfo = repo.GetUserInformationByEmail(commits[i].AuthorEmail);
                        if (authorInfo != null)
                            break;
                    }
                }
            }
            var contributors = (from commit in commits
                                where !string.IsNullOrEmpty(commit.AuthorEmail)
                                let info = repo.GetUserInformationByEmail(commit.AuthorEmail)
                                where info != null
                                group info by info.Id into g
                                select g.First()).ToArray();

            // TODO: support read build history
            var updateAtDateTime = DateTime.Now;
            var culture = LocUtility.GetCultureInfo(document.Docset.Config.Locale);
            model.GitContributorInformation = new GitContributorInfo
            {
                Author = authorInfo?.ToGitUserInfo(),
                Contributors = contributors.Select(ToGitUserInfo).ToArray(),
                UpdatedAtDateTime = updateAtDateTime,
                UpdatedAt = updateAtDateTime.ToString(culture.DateTimeFormat.ShortDatePattern, culture),
            };
            model.Author = authorInfo?.Name;
            model.UpdatedAt = model.GitContributorInformation.UpdatedAt;
        }

        private static GitUserInfo ToGitUserInfo(this GitUserProfile profile)
        {
            return new GitUserInfo
            {
                DisplayName = profile.DisplayName,
                Id = profile.Id,
                ProfileUrl = profile.ProfileUrl,
            };
        }
    }
}
